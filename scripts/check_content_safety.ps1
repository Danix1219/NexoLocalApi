param([string]$EnvFile)
$ErrorActionPreference = 'Stop'
# Match DotEnv: closest existing file, without printing credentials or replacing environment.
if (-not $EnvFile) {
    $directory = Get-Item -LiteralPath (Split-Path -Parent $PSScriptRoot)
    while ($directory) {
        $candidate = Join-Path $directory.FullName '.env'
        if (Test-Path -LiteralPath $candidate) { $EnvFile = $candidate; break }
        $directory = $directory.Parent
    }
}
$settings = @{}
if ($EnvFile -and (Test-Path -LiteralPath $EnvFile)) {
    foreach ($line in Get-Content -LiteralPath $EnvFile) {
        if ($line.Trim().StartsWith('#') -or -not $line.Contains('=')) { continue }
        $pair = $line.Split('=', 2)
        $settings[$pair[0].Trim()] = $pair[1].Trim().Trim('"').Trim("'")
    }
}
foreach ($name in @('ContentSafety__Endpoint','ContentSafety__ApiKey')) {
    $configured = [Environment]::GetEnvironmentVariable($name)
    if ($null -ne $configured) { $settings[$name] = $configured }
    if ([string]::IsNullOrWhiteSpace($settings[$name])) {
        Write-Output "CONTENT-SAFETY-NOT-CONFIGURED: falta $name; no se puede aprobar una publicacion."
        exit 2
    }
}
$endpoint = $null
if (-not [Uri]::TryCreate($settings['ContentSafety__Endpoint'], [UriKind]::Absolute, [ref]$endpoint) -or
    $endpoint.Scheme -ne 'https' -or $endpoint.UserInfo.Length -gt 0) {
    Write-Output 'CONTENT-SAFETY-INVALID-ENDPOINT: se requiere endpoint HTTPS sin credenciales en URL.'
    exit 2
}
try {
    $url = [Uri]::new($endpoint, '/contentsafety/text:analyze?api-version=2024-09-01')
    $body = @{ text = 'Necesito reparar una fuga de agua en la cocina.'; categories = @('Hate','Sexual','Violence','SelfHarm'); outputType = 'FourSeverityLevels' } | ConvertTo-Json -Compress
    $result = Invoke-RestMethod -Uri $url -Method Post -Headers @{ 'Ocp-Apim-Subscription-Key' = $settings['ContentSafety__ApiKey'] } -ContentType 'application/json' -Body $body -TimeoutSec 15
    $rows = @($result.categoriesAnalysis)
    if ($rows.Count -ne 4 -or @($rows.category | Select-Object -Unique).Count -ne 4) { throw 'invalid response' }
    foreach ($category in @('Hate','Sexual','Violence','SelfHarm')) {
        $row = @($rows | Where-Object category -eq $category)
        if ($row.Count -ne 1 -or $row[0].severity -notin @(0,2,4,6)) { throw 'invalid response' }
        if ($row[0].severity -ge 2) { Write-Output 'CONTENT-SAFETY-CHECK-REJECTED: revisar politica antes de publicar.'; exit 1 }
    }
    Write-Output 'CONTENT-SAFETY-OK: respuesta valida; no se creo ninguna publicacion.'
} catch {
    $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 'sin respuesta valida' }
    Write-Output "CONTENT-SAFETY-UNAVAILABLE: $status. Revisar recurso, clave y conectividad."
    exit 1
}
