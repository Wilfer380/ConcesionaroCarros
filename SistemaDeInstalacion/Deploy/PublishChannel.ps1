param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Feature', 'Homologation', 'Production')]
    [string]$Channel,

    [string]$Version = '',
    [string]$BranchName = '',
    [string]$InstallerRoot = ('\\comde019\DFSMDE\PUBLIC\CO_MDE_DISENO_DI\RESPALDO DISE' + [char]0x00D1 + 'OS\SAP - Respaldo dise' + [char]0x00F1 + 'os\FORMATOS SAP\InstallerSystem'),
    [string]$PublishSource = '',
    [string]$LauncherSource = '',
    [string]$ExistingInstallerPath = '',
    [string]$InnoCompilerPath = '',
    [switch]$CompileInstaller,
    [switch]$DryRun
)

<#
Safe channel publisher.

Examples:
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Deploy\PublishChannel.ps1 -Channel Feature -DryRun
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Deploy\PublishChannel.ps1 -Channel Homologation -CompileInstaller -InnoCompilerPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

This script writes under InstallerSystem\Feature, InstallerSystem\Homologation, or InstallerSystem\Production,
and copies the channel-named installer to the root InstallerSystem folder.
It does not delete or promote root InstallerSystem\publish, SetupSistema.exe, version.txt, or release-notes.txt.
Homologation and Production keep the latest 3 semantic-version folders; Feature keeps branch/version folders.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    if ($DryRun) {
        Write-Host "DRY-RUN: $Message"
        return
    }

    Write-Host $Message
}

function Invoke-IfNotDryRun([scriptblock]$Action, [string]$Description) {
    Write-Step $Description
    if (-not $DryRun) {
        & $Action
    }
}

function Format-CommandLine([string]$FilePath, [string[]]$Arguments) {
    $parts = @('"' + $FilePath + '"')
    foreach ($argument in $Arguments) {
        if ($argument -match '\s') {
            $parts += ('"' + ($argument -replace '"', '\"') + '"')
        }
        else {
            $parts += $argument
        }
    }

    return ($parts -join ' ')
}

function Invoke-ExternalCommand([string]$FilePath, [string[]]$Arguments, [string]$Description) {
    Write-Step $Description
    Write-Host ('  ' + (Format-CommandLine $FilePath $Arguments))

    if ($DryRun) { return }

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE"
    }
}

function Get-RepoRoot([string]$StartPath) {
    if ([string]::IsNullOrWhiteSpace($StartPath)) { return $null }
    $dir = New-Object System.IO.DirectoryInfo($StartPath)
    while ($null -ne $dir) {
        if (Test-Path -LiteralPath (Join-Path $dir.FullName '.git')) { return $dir.FullName }
        $dir = $dir.Parent
    }
    return $null
}

function Invoke-GitLine([string[]]$GitArgs, [string]$WorkingDirectory) {
    if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) { return $null }
    Push-Location $WorkingDirectory
    try {
        $result = & git @GitArgs 2>$null
        if ($LASTEXITCODE -ne 0 -or $null -eq $result) { return $null }
        return ($result | Select-Object -First 1).ToString().Trim()
    }
    finally {
        Pop-Location
    }
}

function Get-ReleaseVersion([string]$ProjectPath) {
    if (-not (Test-Path -LiteralPath $ProjectPath)) {
        throw "No existe el csproj para leer ReleaseVersion: $ProjectPath"
    }

    $content = Get-Content -LiteralPath $ProjectPath -Raw
    $match = [Regex]::Match($content, '<ReleaseVersion>([^<]+)</ReleaseVersion>')
    if (-not $match.Success) {
        throw "No pude encontrar <ReleaseVersion> en $ProjectPath"
    }

    return $match.Groups[1].Value.Trim()
}

function ConvertTo-SafePathName([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return 'unknown' }
    $safe = $Value.Trim() -replace '[\\/:*?"<>|\s]+', '-'
    $safe = $safe -replace '[^A-Za-z0-9._-]', '-'
    $safe = $safe.Trim('.-')
    if ([string]::IsNullOrWhiteSpace($safe)) { return 'unknown' }
    return $safe
}

function Get-SemVerOrNull([string]$Name) {
    $clean = $Name.Trim()
    if ($clean -notmatch '^\d+(\.\d+){2,3}$') { return $null }
    try { return [Version]$clean } catch { return $null }
}

function Remove-OldChannelVersions([string]$ChannelRoot, [int]$Keep) {
    if ($Keep -lt 1 -or -not (Test-Path -LiteralPath $ChannelRoot)) { return }

    $versions = @(
        Get-ChildItem -LiteralPath $ChannelRoot -Directory |
            ForEach-Object {
                $parsed = Get-SemVerOrNull $_.Name
                if ($null -ne $parsed) {
                    [PSCustomObject]@{ Directory = $_; Version = $parsed }
                }
            } |
            Sort-Object -Property Version -Descending
    )

    $toRemove = @($versions | Select-Object -Skip $Keep)
    foreach ($entry in $toRemove) {
        Invoke-IfNotDryRun { Remove-Item -LiteralPath $entry.Directory.FullName -Recurse -Force } "Removing old $Channel version folder: $($entry.Directory.FullName)"
    }
}

function Copy-DirectoryContents([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source)) { throw "No existe la carpeta origen: $Source" }

    Invoke-IfNotDryRun { New-Item -ItemType Directory -Path $Destination -Force | Out-Null } "Ensuring folder: $Destination"
    Invoke-IfNotDryRun { Copy-Item -Path (Join-Path $Source '*') -Destination $Destination -Recurse -Force } "Copying $Source -> $Destination"
}

function Write-TextFile([string]$Path, [string]$Content) {
    Invoke-IfNotDryRun {
        $dir = Split-Path -Parent $Path
        if (-not [string]::IsNullOrWhiteSpace($dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
        [System.IO.File]::WriteAllText($Path, $Content, (New-Object System.Text.UTF8Encoding($false)))
    } "Writing metadata: $Path"
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Get-RepoRoot $PSScriptRoot
$csprojPath = Join-Path $projectRoot 'SistemaDeInstalacion.csproj'

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-ReleaseVersion $csprojPath
}

if ([string]::IsNullOrWhiteSpace($BranchName)) {
    $BranchName = Invoke-GitLine @('rev-parse', '--abbrev-ref', 'HEAD') $repoRoot
}

if ([string]::IsNullOrWhiteSpace($BranchName)) {
    $BranchName = 'manual'
}

if ([string]::IsNullOrWhiteSpace($PublishSource)) {
    $PublishSource = Join-Path $InstallerRoot 'publish'
}

if ([string]::IsNullOrWhiteSpace($LauncherSource)) {
    $LauncherSource = Join-Path $InstallerRoot 'LauncherSistema'
}

if ([string]::IsNullOrWhiteSpace($ExistingInstallerPath)) {
    $ExistingInstallerPath = Join-Path $InstallerRoot 'SetupSistema.exe'
}

$safeBranch = ConvertTo-SafePathName $BranchName
$safeVersion = ConvertTo-SafePathName $Version
$channelRoot = Join-Path $InstallerRoot $Channel

if ($Channel -eq 'Feature') {
    $destinationRoot = Join-Path (Join-Path $channelRoot $safeBranch) $safeVersion
}
else {
    $destinationRoot = Join-Path $channelRoot $safeVersion
}

$installerBaseName = $Channel
$rootChannelInstaller = Join-Path $InstallerRoot ($installerBaseName + '.exe')

$destinationPublish = Join-Path $destinationRoot 'publish'
$destinationLauncher = Join-Path $destinationRoot 'LauncherSistema'
$destinationVersion = Join-Path $destinationRoot 'version.txt'
$destinationReleaseNotes = Join-Path $destinationRoot 'release-notes.txt'
$sourceReleaseNotes = Join-Path $InstallerRoot 'release-notes.txt'

Write-Host "Publishing channel artifact"
Write-Host "  Channel: $Channel"
Write-Host "  Version: $Version"
Write-Host "  Branch : $BranchName"
Write-Host "  Target : $destinationRoot"

Invoke-IfNotDryRun { New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null } "Ensuring channel/version folder: $destinationRoot"
Copy-DirectoryContents $PublishSource $destinationPublish
Copy-DirectoryContents $LauncherSource $destinationLauncher

Write-TextFile $destinationVersion ($Version + "`r`n")
Write-TextFile (Join-Path $destinationRoot 'channel.txt') ($Channel + "`r`n")
Write-TextFile (Join-Path $destinationRoot 'branch.txt') ($BranchName + "`r`n")
Write-TextFile (Join-Path $destinationRoot 'deployed-at.txt') ((Get-Date).ToString('yyyy-MM-dd HH:mm:ss') + "`r`n")

if (Test-Path -LiteralPath $sourceReleaseNotes) {
    Invoke-IfNotDryRun { Copy-Item -LiteralPath $sourceReleaseNotes -Destination $destinationReleaseNotes -Force } "Copying release notes -> $destinationReleaseNotes"
}
else {
    Write-TextFile $destinationReleaseNotes "Cambios incluidos en esta version ($Version):`r`n- Release notes no disponibles.`r`n"
}

if ($CompileInstaller) {
    if ([string]::IsNullOrWhiteSpace($InnoCompilerPath)) {
        $InnoCompilerPath = 'ISCC.exe'
    }

    $issPath = Join-Path $PSScriptRoot 'SetupSistema.iss'
    $innoArgs = @(
        "/DSourceRoot=$destinationRoot",
        "/DOutputDir=$destinationRoot",
        "/DOutputBaseFilename=$installerBaseName",
        "/DChannelName=$Channel",
        "/DServerVersionFile=$destinationVersion",
        "/DReleaseNotesOut=$destinationReleaseNotes",
        "/DNoVersionIncrement=1",
        $issPath
    )

    Invoke-ExternalCommand $InnoCompilerPath $innoArgs "Compiling channel installer with Inno Setup: $installerBaseName.exe"

    $destinationInstaller = Join-Path $destinationRoot ($installerBaseName + '.exe')
    if (-not $DryRun -and -not (Test-Path -LiteralPath $destinationInstaller)) {
        throw "Inno Setup completed but expected installer was not created: $destinationInstaller"
    }

    Invoke-IfNotDryRun { Copy-Item -LiteralPath $destinationInstaller -Destination $rootChannelInstaller -Force } "Copying channel installer to root InstallerSystem folder -> $rootChannelInstaller"
}
elseif (Test-Path -LiteralPath $ExistingInstallerPath) {
    $destinationInstaller = Join-Path $destinationRoot ($installerBaseName + '.exe')
    Invoke-IfNotDryRun { Copy-Item -LiteralPath $ExistingInstallerPath -Destination $destinationInstaller -Force } "Copying existing installer artifact -> $destinationInstaller"
    Invoke-IfNotDryRun { Copy-Item -LiteralPath $ExistingInstallerPath -Destination $rootChannelInstaller -Force } "Copying existing installer artifact to root InstallerSystem folder -> $rootChannelInstaller"
}
else {
    Write-Warning "No installer copied. Existing installer not found: $ExistingInstallerPath. Use -CompileInstaller with -InnoCompilerPath to generate one."
}

if ($Channel -eq 'Homologation' -or $Channel -eq 'Production') {
    Remove-OldChannelVersions $channelRoot 3
}

Write-Host "OK. Channel artifact ready: $destinationRoot"
