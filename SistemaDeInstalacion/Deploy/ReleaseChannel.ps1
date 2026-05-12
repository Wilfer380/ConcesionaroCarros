param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Feature', 'Homologation', 'Production')]
    [string]$Channel,

    [string]$Configuration = 'Release',
    [string]$Platform = 'AnyCPU',
    [string]$InstallerRoot = ('\\comde019\DFSMDE\PUBLIC\CO_MDE_DISENO_DI\RESPALDO DISE' + [char]0x00D1 + 'OS\SAP - Respaldo dise' + [char]0x00F1 + 'os\FORMATOS SAP\InstallerSystem'),
    [string]$MsBuildPath = '',
    [string]$InnoCompilerPath = '',
    [switch]$ConfirmProduction,
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$SkipInstallerCompile
)

<#
One-command release automation for SistemaDeInstalacion channel publishing.
    Restores and builds the .NET Framework projects with the RID MSBuild resolves for these PackageReference assets.

Example:
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Deploy\ReleaseChannel.ps1 -Channel Homologation

Production requires an explicit safety switch:
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Deploy\ReleaseChannel.ps1 -Channel Production -ConfirmProduction
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-MsBuildPath([string]$RequestedPath) {
    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) { return $RequestedPath }

    $candidates = @(
        'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }

    return 'msbuild.exe'
}

function Resolve-InnoCompilerPath([string]$RequestedPath) {
    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) { return $RequestedPath }

    $defaultPath = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
    if (Test-Path -LiteralPath $defaultPath) { return $defaultPath }

    return 'ISCC.exe'
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
    Write-Host $Description
    Write-Host ('  ' + (Format-CommandLine $FilePath $Arguments))

    if ($DryRun) { return }

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE"
    }
}

if ($Channel -eq 'Production' -and -not $ConfirmProduction) {
    throw 'Production publishing is blocked unless -ConfirmProduction is supplied.'
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$sistemaProject = Join-Path $projectRoot 'SistemaDeInstalacion.csproj'
$launcherProject = Join-Path $projectRoot 'LauncherSistema\LauncherSistema.csproj'
$setupScript = Join-Path $PSScriptRoot 'SetupSistema.iss'
$publishScript = Join-Path $PSScriptRoot 'PublishChannel.ps1'
$rootSetupInstaller = Join-Path $InstallerRoot 'SetupSistema.exe'
$rootChannelInstaller = Join-Path $InstallerRoot ($Channel + '.exe')
$channelRoot = Join-Path $InstallerRoot $Channel

if (-not (Test-Path -LiteralPath $sistemaProject)) { throw "Missing project file: $sistemaProject" }
if (-not (Test-Path -LiteralPath $launcherProject)) { throw "Missing project file: $launcherProject" }
if (-not (Test-Path -LiteralPath $setupScript)) { throw "Missing Inno Setup script: $setupScript" }
if (-not (Test-Path -LiteralPath $publishScript)) { throw "Missing publish script: $publishScript" }

$resolvedMsBuild = Resolve-MsBuildPath $MsBuildPath
$resolvedInnoCompiler = Resolve-InnoCompilerPath $InnoCompilerPath

Write-Host 'Release channel automation'
Write-Host "  Channel            : $Channel"
Write-Host "  Configuration      : $Configuration"
Write-Host "  Platform           : $Platform"
Write-Host "  InstallerRoot      : $InstallerRoot"
Write-Host "  Root setup output  : $rootSetupInstaller"
Write-Host "  Channel root output: $rootChannelInstaller"
Write-Host "  Channel folder     : $channelRoot"
if ($DryRun) { Write-Host '  Mode               : DryRun (commands will not execute)' }

if (-not $SkipBuild) {
    $restoreRuntimeIdentifier = 'win'

    $restoreArgs = @(
        $sistemaProject,
        '/t:Restore',
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:RuntimeIdentifier=$restoreRuntimeIdentifier",
        "/p:RuntimeIdentifiers=$restoreRuntimeIdentifier"
    )
    Invoke-ExternalCommand $resolvedMsBuild $restoreArgs 'Restoring SistemaDeInstalacion packages'

    $buildArgs = @(
        $sistemaProject,
        '/t:Build',
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:RuntimeIdentifier=$restoreRuntimeIdentifier",
        "/p:RuntimeIdentifiers=$restoreRuntimeIdentifier",
        "/p:InstallerRoot=$InstallerRoot"
    )
    Invoke-ExternalCommand $resolvedMsBuild $buildArgs 'Building SistemaDeInstalacion'

    $launcherRestoreArgs = @(
        $launcherProject,
        '/t:Restore',
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:RuntimeIdentifier=$restoreRuntimeIdentifier",
        "/p:RuntimeIdentifiers=$restoreRuntimeIdentifier"
    )
    Invoke-ExternalCommand $resolvedMsBuild $launcherRestoreArgs 'Restoring LauncherSistema packages'

    $launcherBuildArgs = @(
        $launcherProject,
        '/t:Build',
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:RuntimeIdentifier=$restoreRuntimeIdentifier",
        "/p:RuntimeIdentifiers=$restoreRuntimeIdentifier",
        "/p:InstallerRoot=$InstallerRoot"
    )
    Invoke-ExternalCommand $resolvedMsBuild $launcherBuildArgs 'Building LauncherSistema'
}
else {
    Write-Host 'Skipping project builds.'
}

if (-not $SkipInstallerCompile) {
    $innoArgs = @(
        "/DInstallerRoot=$InstallerRoot",
        "/DSourceRoot=$InstallerRoot",
        "/DOutputDir=$InstallerRoot",
        '/DOutputBaseFilename=SetupSistema',
        $setupScript
    )
    Invoke-ExternalCommand $resolvedInnoCompiler $innoArgs 'Compiling root SetupSistema installer'
}
else {
    Write-Host 'Skipping root installer compile.'
}

$publishArgs = @(
    '-NoProfile',
    '-ExecutionPolicy',
    'Bypass',
    '-File',
    $publishScript,
    '-Channel',
    $Channel,
    '-InstallerRoot',
    $InstallerRoot
)
if ($DryRun) { $publishArgs += '-DryRun' }
if (-not $SkipInstallerCompile) {
    $publishArgs += '-CompileInstaller'
    $publishArgs += '-InnoCompilerPath'
    $publishArgs += $resolvedInnoCompiler
}

Invoke-ExternalCommand 'powershell.exe' $publishArgs 'Publishing channel artifact'

Write-Host 'OK. Release channel automation completed.'
Write-Host "  Preserved root setup installer: $rootSetupInstaller"
Write-Host "  Published channel root exe   : $rootChannelInstaller"
Write-Host "  Published channel folder     : $channelRoot"
