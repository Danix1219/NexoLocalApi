#Requires -Version 5.1
# Lint documental inspirado en check_sdd.ps1 del profesor.
# PASS no significa build/tests/API/Flutter aprobados.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$isApi = Test-Path -LiteralPath (Join-Path $root '.specify/memory/constitution.md')
$specRoot = Join-Path $root 'specs'
$pattern = if ($isApi) { '^nv-\d{2}$' } else { '^\d{3}-[a-z0-9-]+$' }
$taskName = if ($isApi) { 'task.md' } else { 'tasks.md' }
$checkName = if ($isApi) { 'checklists/gates.md' } else { 'checklist.md' }
$failures = New-Object System.Collections.Generic.List[string]
$specs = @(Get-ChildItem -LiteralPath $specRoot -Directory | Where-Object { $_.Name -match $pattern })
if ($specs.Count -eq 0) { $failures.Add('No hay specs vigentes.') }
foreach ($dir in $specs) {
    foreach ($name in @('spec.md','plan.md',$taskName,$checkName)) {
        if (-not (Test-Path -LiteralPath (Join-Path $dir.FullName $name))) { $failures.Add("$($dir.Name): falta $name") }
    }
    $specFile = Join-Path $dir.FullName 'spec.md'
    if (-not (Test-Path -LiteralPath $specFile)) { continue }
    $spec = Get-Content -LiteralPath $specFile -Raw -Encoding UTF8
    foreach ($n in 1..7) {
        if ($spec -notmatch "(?m)^## $n\. ") { $failures.Add("$($dir.Name): falta seccion $n") }
    }
    if ($spec -notmatch '(?m)^## Aprobaci') { $failures.Add("$($dir.Name): falta Aprobacion") }
    if ($spec -notmatch '(?m)^Estado: Borrador documental;') { $failures.Add("$($dir.Name): estado documental inesperado; revisar manualmente") }
    $fr = @([regex]::Matches($spec,'(?m)^- ((?:FR|NFR)-\d{3}):') | ForEach-Object { $_.Groups[1].Value })
    $tasks = Get-Content -LiteralPath (Join-Path $dir.FullName $taskName) -Raw -Encoding UTF8
    foreach ($line in ($tasks -split "`n" | Where-Object { $_ -match '^- \[ \] T-\d{3}' })) {
        if ($line -notmatch '(FR|NFR)-\d{3}' -or $line -notmatch 'Acepta:') { $failures.Add("$($dir.Name): tarea sin requisito o aceptacion") }
        foreach ($m in [regex]::Matches($line,'(?:FR|NFR)-\d{3}')) {
            if ($fr -notcontains $m.Value) { $failures.Add("$($dir.Name): tarea referencia $($m.Value) inexistente") }
        }
    }
    if ($tasks -match '(?m)^- \[[xX]\]') { $failures.Add("$($dir.Name): tarea marcada ejecutada en fase documental") }
}
# Enlaces Markdown relativos en ambos contextos. No abre red ni enlaces externos.
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -Filter '*.md') {
    $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach ($match in [regex]::Matches($text,'\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Split('#')[0]
        if (-not $target -or $target -match '^(https?:|[A-Za-z]:|/)') { continue }
        if (-not (Test-Path -LiteralPath (Join-Path $file.DirectoryName $target))) { $failures.Add("$($file.Name): enlace roto $target") }
    }
}
if ($isApi) {
    $catalog = Get-Content -LiteralPath (Join-Path $root 'docs/contracts/inventario.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $ids = @($catalog.operations | ForEach-Object { $_.id })
    if (@($ids | Select-Object -Unique).Count -ne $ids.Count) { $failures.Add('EP duplicados en catalogo.') }
    $endpoints = Get-Content -LiteralPath (Join-Path $root 'docs/endpoints.md') -Raw -Encoding UTF8
    foreach ($operation in $catalog.operations) {
        if (-not (Test-Path -LiteralPath (Join-Path $specRoot "$($operation.spec)/spec.md"))) { $failures.Add("$($operation.id): spec viva inexistente") }
        if ($endpoints -notmatch [regex]::Escape($operation.id)) { $failures.Add("$($operation.id): falta indice endpoints") }
    }
    if ($catalog.count -ne @($catalog.operations).Count) { $failures.Add('Cantidad declarada no coincide con inventario.') }
}
if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Output "SDD-FAIL: $_" }
    exit 1
}
Write-Output "SDD-OK: $($specs.Count) specs; estructura/trazabilidad/enlaces documentales. Sin validacion de producto."
exit 0
