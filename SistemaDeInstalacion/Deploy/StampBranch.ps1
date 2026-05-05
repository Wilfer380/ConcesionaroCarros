param(
    [string]$OutFile,
    [string]$Fallback = "RELEASE"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Find-GitRoot([string]$startPath) {
    if ([string]::IsNullOrWhiteSpace($startPath)) { return $null }
    $dir = New-Object System.IO.DirectoryInfo($startPath)
    while ($dir -ne $null) {
        $gitDir = Join-Path $dir.FullName ".git"
        if (Test-Path -LiteralPath $gitDir) { return $dir.FullName }
        $dir = $dir.Parent
    }
    return $null
}

if ([string]::IsNullOrWhiteSpace($OutFile)) { throw "OutFile es requerido" }

$repoRoot = Find-GitRoot (Split-Path -Parent $PSScriptRoot)
$branch = $Fallback

if (-not [string]::IsNullOrWhiteSpace($repoRoot)) {
    Push-Location $repoRoot
    try {
        $b = & git rev-parse --abbrev-ref HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($b)) {
            $branch = $b.ToString().Trim()
        }
    } finally {
        Pop-Location
    }
}

$dir = Split-Path -Parent $OutFile
if (-not [string]::IsNullOrWhiteSpace($dir) -and -not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Path $dir | Out-Null
}

# Guardar ASCII/UTF8 sin BOM: solo texto de rama.
[System.IO.File]::WriteAllText($OutFile, ($branch + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "OK. Branch stamp: $branch -> $OutFile"
