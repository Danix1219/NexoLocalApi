param([int]$TimeoutSeconds = 30)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -LiteralPath (Join-Path $root 'scaffold.json') -Raw | ConvertFrom-Json
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$logRoot = Join-Path $root ('artifacts/smoke/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$started = @()
# El smoke solo mide que el host arranca: una cadena local ficticia satisface la validacion de arranque
# sin conectar a Atlas ni versionar secreto alguno. Si el entorno ya trae una, se respeta.
if (-not $env:Mongo__ConnectionString) { $env:Mongo__ConnectionString = 'mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=500&connectTimeoutMS=500' }
try {
    # Check ports before starting: an existing process is never stopped or used as evidence.
    foreach ($entry in $manifest.hosts | Where-Object kind -eq 'http') {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, [int]$entry.port)
        try { $listener.Start() } finally { $listener.Stop() }
    }
    foreach ($entry in $manifest.hosts) {
        $project = Join-Path $root $entry.project
        $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $dll = Join-Path (Split-Path $project -Parent) "bin/Release/net10.0/$name.dll"
        if (-not (Test-Path -LiteralPath $dll)) { throw "Build Release required: $dll" }
        $arguments = @('"' + $dll + '"')
        if ($entry.kind -eq 'http') { $arguments += @('--urls', "http://127.0.0.1:$($entry.port)") }
        $windowOptions = @{}
        if ($env:OS -eq 'Windows_NT') { $windowOptions.WindowStyle = 'Hidden' }
        $process = Start-Process -FilePath $dotnet -ArgumentList $arguments -WorkingDirectory (Split-Path $project -Parent) @windowOptions -PassThru -RedirectStandardOutput (Join-Path $logRoot "$name.stdout.log") -RedirectStandardError (Join-Path $logRoot "$name.stderr.log")
        $started += [pscustomobject]@{ Process = $process; Entry = $entry }
    }
    foreach ($item in $started) {
        $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
        $ready = $false
        while ([DateTime]::UtcNow -lt $deadline) {
            $item.Process.Refresh()
            if ($item.Process.HasExited) { throw "Host exited: $($item.Entry.project); inspect $logRoot" }
            if ($item.Entry.kind -eq 'worker') {
                # Only process lifetime; no consumer or delivery functionality is claimed.
                $ready = $true
                break
            }
            try {
                $response = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$($item.Entry.port)/" -TimeoutSec 2
                throw "Unexpected endpoint/status: $($response.StatusCode)"
            } catch {
                if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) { $ready = $true; break }
                if ($_.Exception.Message -like 'Unexpected endpoint/status:*') { throw }
            }
            Start-Sleep -Milliseconds 200
        }
        if (-not $ready) { throw "Host timeout: $($item.Entry.project)" }
    }
    foreach ($item in $started) {
        $item.Process.Refresh()
        if ($item.Process.HasExited) { throw "Host exited during concurrent check: $($item.Entry.project)" }
    }
    Write-Output "SCAFFOLD-HOSTS-OK: 9 HTTP hosts return expected 404; S07 Worker alive; no business/integration acceptance. Logs: $logRoot"
} finally {
    foreach ($item in $started) {
        $item.Process.Refresh()
        if (-not $item.Process.HasExited) { Stop-Process -Id $item.Process.Id -Force -ErrorAction SilentlyContinue }
        $item.Process.Dispose()
    }
}
