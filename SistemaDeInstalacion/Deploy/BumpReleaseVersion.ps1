param(
    [string]$CsprojPath,
    [string]$OutVersionFile,
    [switch]$NoIncrement
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Bump-SemVer([string]$v) {
    $maxPatchCounter = 30

    if ([string]::IsNullOrWhiteSpace($v)) { throw "Version vacía" }
    $parts = $v.Trim().Split('.')
    if ($parts.Length -ne 4) { throw "La versión debe tener 4 partes: 1.0.0.0 (actual: $v)" }
    $nums = @()
    foreach ($p in $parts) {
        $n = 0
        if (-not [int]::TryParse($p, [ref]$n)) { throw "Parte inválida en versión: $v" }
        if ($n -lt 0) { throw "Parte negativa en versión: $v" }
        $nums += $n
    }

    # The fourth segment is a bounded release counter, not an unbounded semver patch.
    if ($nums[3] -lt $maxPatchCounter) {
        $nums[3] = $nums[3] + 1
    } else {
        $nums[2] = $nums[2] + 1
        $nums[3] = 0
    }

    return ($nums -join '.')
}

if (-not (Test-Path -LiteralPath $CsprojPath)) { throw "No existe csproj: $CsprojPath" }
if ([string]::IsNullOrWhiteSpace($OutVersionFile)) { throw "OutVersionFile es requerido" }

$csproj = Get-Content -LiteralPath $CsprojPath -Raw
$m = [Regex]::Match($csproj, '<ReleaseVersion>([^<]+)</ReleaseVersion>')
if (-not $m.Success) { throw "No pude encontrar <ReleaseVersion> en $CsprojPath" }

$current = $m.Groups[1].Value.Trim()
$next = $current
if (-not $NoIncrement) {
    $next = Bump-SemVer $current
}

if ($next -ne $current) {
    $csproj2 = [Regex]::Replace(
        $csproj,
        '<ReleaseVersion>[^<]+</ReleaseVersion>',
        "<ReleaseVersion>$next</ReleaseVersion>",
        1
    )

    [System.IO.File]::WriteAllText($CsprojPath, $csproj2, (New-Object System.Text.UTF8Encoding($true)))
}

try {
    $outDir = Split-Path -Parent $OutVersionFile
    if (-not [string]::IsNullOrWhiteSpace($outDir) -and -not (Test-Path -LiteralPath $outDir)) {
        New-Item -ItemType Directory -Path $outDir | Out-Null
    }
    [System.IO.File]::WriteAllText($OutVersionFile, ($next + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
} catch {
    throw "No pude escribir OutVersionFile: $OutVersionFile"
}

if ($NoIncrement) {
    Write-Host "OK. Versión sincronizada: $next"
} else {
    Write-Host "OK. Versión actual: $current -> Nueva versión: $next"
}
