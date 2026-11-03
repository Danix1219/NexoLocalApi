#Requires -Version 5.1
param([Parameter(Mandatory = $true)][string]$WorkspaceRoot)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$config = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'context-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$workspace = [IO.Path]::GetFullPath($WorkspaceRoot).TrimEnd('\','/')
$expectedRoot = [IO.Path]::GetFullPath((Join-Path $workspace $config.repoFolder))
if ([IO.Path]::GetFullPath($repoRoot) -ne $expectedRoot) { throw 'Checkout folder does not match the declared repository layout.' }
$archive = Join-Path $PSScriptRoot 'sdd-context.zip'
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $config.archiveSha256) { throw 'Documentation context archive hash mismatch.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Replace('\','/')
        $top = $relative.Split('/')[0]
        if ($config.allowedRoots -notcontains $top -or $top -eq $config.repoFolder) { throw 'Unexpected context root.' }
        $resolved = [IO.Path]::GetFullPath((Join-Path $workspace $relative))
        if (-not $resolved.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Context archive path escapes the workspace.' }
        if ($entry.Name) {
            $directory = Split-Path -Parent $resolved
            if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $resolved, $true)
        }
    }
} finally { $zip.Dispose() }
foreach ($file in $config.files) {
    $target = Join-Path $workspace $file.path
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw 'Extracted documentation reference hash mismatch.' }
}
# Documentation CI must never turn into a green product gate by silently ignoring new application code.
$manifests = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -File | Where-Object { $_.Extension -eq '.csproj' -or $_.Name -eq 'pubspec.yaml' })
if ($manifests.Count -gt 0) {
    $productConfigPath = Join-Path $PSScriptRoot 'product-ci.json'
    if (-not (Test-Path -LiteralPath $productConfigPath)) { throw 'Product manifest detected without explicit product CI configuration.' }
    $productConfig = Get-Content -LiteralPath $productConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($productConfig.solution -ne 'nexolocal.Api.slnx' -or $manifests.Count -ne $productConfig.expectedProjects -or @($manifests | Where-Object Name -eq 'pubspec.yaml').Count -gt 0) { throw 'Unexpected product manifests: review the backend scaffold CI policy.' }
    $productWorkflow = Get-Content -LiteralPath (Join-Path $repoRoot $productConfig.workflow) -Raw -Encoding UTF8
    foreach ($command in @('dotnet restore nexolocal.Api.slnx','dotnet build nexolocal.Api.slnx','scripts/check_architecture.py','scripts/Test-ScaffoldHosts.ps1')) {
        if (-not $productWorkflow.Contains($command)) { throw "Product CI missing command: $command" }
    }
    $sddWorkflow = Get-Content -LiteralPath (Join-Path $repoRoot '.github/workflows/sdd.yml') -Raw -Encoding UTF8
    if (-not $sddWorkflow.Contains('needs: product') -or -not $sddWorkflow.Contains('uses: ./.github/workflows/backend-base.yml')) { throw 'Documentary CI must depend on the product workflow.' }
    Write-Output 'PRODUCT-CI-CONFIG-OK: explicit scaffold CI required before SDD job; not business acceptance.'
}
Push-Location -LiteralPath $repoRoot
try {
    & powershell -NoProfile -File scripts/check_sdd.ps1
    if ($LASTEXITCODE -ne 0) { throw 'SDD structure/link validation failed.' }
} finally { Pop-Location }
$isApi = Test-Path -LiteralPath (Join-Path $repoRoot '.specify/memory/constitution.md')
$pattern = if ($isApi) { '^nv-\d{2}$' } else { '^\d{3}-[a-z0-9-]+$' }
$count = 0
foreach ($dir in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'specs') -Directory | Where-Object { $_.Name -match $pattern }) {
    $count++
    $taskName = if ($isApi) { 'task.md' } else { 'tasks.md' }
    $checkName = if ($isApi) { 'checklists/gates.md' } else { 'checklist.md' }
    $spec = Get-Content -LiteralPath (Join-Path $dir.FullName 'spec.md') -Raw -Encoding UTF8
    $tasks = Get-Content -LiteralPath (Join-Path $dir.FullName $taskName) -Raw -Encoding UTF8
    $all = $spec + "`n" + $tasks + "`n" + (Get-Content -LiteralPath (Join-Path $dir.FullName 'plan.md') -Raw -Encoding UTF8) + "`n" + (Get-Content -LiteralPath (Join-Path $dir.FullName $checkName) -Raw -Encoding UTF8)
    $taskIds = @([regex]::Matches($tasks,'(?m)^- \[ \] (T-\d{3}) ') | ForEach-Object { $_.Groups[1].Value })
    $requirements = @([regex]::Matches($spec,'(?m)^- ((?:FR|NFR)-\d{3}):') | ForEach-Object { $_.Groups[1].Value })
    $criteria = @([regex]::Matches($spec,'(?m)^- \[ \] (AC-\d{3})') | ForEach-Object { $_.Groups[1].Value })
    foreach ($values in @(@{name='tasks';items=$taskIds},@{name='requirements';items=$requirements},@{name='criteria';items=$criteria})) {
        if (@($values.items | Select-Object -Unique).Count -ne @($values.items).Count) { throw "$($dir.Name): duplicate $($values.name)." }
    }
    foreach ($match in [regex]::Matches($all,'\bT-\d{3}\b')) { if ($taskIds -notcontains $match.Value) { throw "$($dir.Name): undefined task $($match.Value)." } }
    foreach ($id in $requirements) {
        if ($tasks -notmatch ('\b' + [regex]::Escape($id) + '\b') -or $spec -notmatch ('(?m)^\| ' + [regex]::Escape($id) + ' \|')) { throw "$($dir.Name): requirement without task/matrix: $id." }
    }
    foreach ($id in $criteria) { if ($tasks -notmatch ('\b' + [regex]::Escape($id) + '\b')) { throw "$($dir.Name): criterion without task: $id." } }
    foreach ($match in [regex]::Matches($tasks,'\b(?:FR|NFR)-\d{3}\b')) { if ($requirements -notcontains $match.Value) { throw "$($dir.Name): undefined requirement $($match.Value)." } }
    foreach ($match in [regex]::Matches($tasks,'\bAC-\d{3}\b')) { if ($criteria -notcontains $match.Value) { throw "$($dir.Name): undefined criterion $($match.Value)." } }
}
Write-Output "SDD-TRACE-OK: $count specs; documentary IDs and coverage. No product acceptance."

# Backend documentary graph validation (not a product gate).
& python -B -m unittest discover -s (Join-Path $repoRoot 'scripts') -p 'test_sdd_integrity.py'
if ($LASTEXITCODE -ne 0) { throw 'SDD integrity validator regression tests failed.' }
& python -B (Join-Path $repoRoot 'scripts/check_sdd_integrity.py')
if ($LASTEXITCODE -ne 0) { throw 'SDD task graph/coverage validation failed.' }
