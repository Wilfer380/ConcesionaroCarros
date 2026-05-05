param(
    [string]$IssPath = "$(Join-Path $PSScriptRoot 'SetupSistema.iss')",
    [string]$OutFile = "$(Join-Path $PSScriptRoot 'release-notes.txt')",
    [string]$StateFile = "$(Join-Path $PSScriptRoot 'release-notes.state.json')",
    [string]$ServerVersionFile = "",
    [string]$AppVersion = "",
    [int]$MaxCommits = 80
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-AppVersionFromIss([string]$issContent) {
    $m = [Regex]::Match($issContent, '^\s*AppVersion\s*=\s*(.+)\s*$', 'Multiline')
    if (-not $m.Success) { return $null }
    return $m.Groups[1].Value.Trim()
}

function Invoke-Git([string[]]$GitArgs) {
    try {
        # Ejecutamos git pero suprimimos stderr y evitamos que PowerShell trate stderr como error record.
        $prevPreference = $ErrorActionPreference
        $ErrorActionPreference = "SilentlyContinue"
        try {
            $stdout = & git @GitArgs 2>$null
        }
        finally {
            $ErrorActionPreference = $prevPreference
        }

        if ($LASTEXITCODE -ne 0) {
            return @()
        }

        if ($null -eq $stdout) { return @() }

        $lines = @()
        if ($stdout -is [string]) { $lines = @($stdout) } else { $lines = @($stdout) }
        $lines = $lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

        # Protección: si por algún motivo Git devolvió el texto de ayuda/usage, lo tratamos como error.
        if ($lines.Count -gt 0 -and ($lines[0].ToString().Trim().ToLowerInvariant().StartsWith("usage: git"))) {
            return @()
        }

        return @($lines)
    }
    catch {
        return @()
    }
}

function Is-CommitHash([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $false }
    return [Regex]::IsMatch($value.Trim(), '^[0-9a-f]{7,40}$', 'IgnoreCase')
}

function Load-State([string]$path) {
    try {
        if (-not (Test-Path -LiteralPath $path)) { return $null }
        $raw = Get-Content -LiteralPath $path -Raw
        if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
        $s = ($raw | ConvertFrom-Json)
        if ($s -and $s.lastCommit -and -not (Is-CommitHash ($s.lastCommit.ToString()))) {
            return $null
        }
        return $s
    } catch {
        return $null
    }
}

function Save-State([string]$path, [string]$version, [string]$commit) {
    if (-not (Is-CommitHash $commit)) { return }
    $state = [ordered]@{
        lastVersion = $version
        lastCommit  = $commit
        generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    }
    $json = ($state | ConvertTo-Json -Depth 5)
    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($path, $json + "`r`n", $utf8Bom)
}

function Is-Ancestor([string]$maybeAncestor, [string]$commit) {
    if (-not (Is-CommitHash $maybeAncestor) -or -not (Is-CommitHash $commit)) { return $false }
    & git merge-base --is-ancestor $maybeAncestor $commit 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Get-ChangedFiles([string]$baseCommit, [string]$headCommit, [int]$fallbackCommits) {
    $files = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)

    if (-not [string]::IsNullOrWhiteSpace($baseCommit) -and (Is-Ancestor $baseCommit $headCommit)) {
        $diff = Invoke-Git @('diff','--name-only',"$baseCommit..$headCommit")
        foreach ($f in $diff) {
            $t = ""
            if ($null -ne $f) { $t = ($f.ToString()).Trim() }
            if ($t) { [void]$files.Add($t) }
        }
        return $files
    }

    # Fallback: tomar archivos tocados en los últimos N commits (útil si no hay state o no hay historial limpio).
    $log = Invoke-Git @('log',"-n",$fallbackCommits.ToString(),'--name-only','--pretty=format:')
    foreach ($f in $log) {
        $t = ""
        if ($null -ne $f) { $t = ($f.ToString()).Trim() }
        if ($t) { [void]$files.Add($t) }
    }
    return $files
}

function Get-WorkingTreeChangedFiles() {
    $files = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)

    # Unstaged changes
    $unstaged = Invoke-Git @('diff','--name-only')
    foreach ($f in $unstaged) {
        $t = ""
        if ($null -ne $f) { $t = ($f.ToString()).Trim() }
        if ($t) { [void]$files.Add($t) }
    }

    # Staged changes
    $staged = Invoke-Git @('diff','--cached','--name-only')
    foreach ($f in $staged) {
        $t = ""
        if ($null -ne $f) { $t = ($f.ToString()).Trim() }
        if ($t) { [void]$files.Add($t) }
    }

    # Untracked files
    $untracked = Invoke-Git @('ls-files','--others','--exclude-standard')
    foreach ($f in $untracked) {
        $t = ""
        if ($null -ne $f) { $t = ($f.ToString()).Trim() }
        if ($t) { [void]$files.Add($t) }
    }

    return $files
}

function Is-WorkingTreeDirty() {
    $status = Invoke-Git @('status','--porcelain')
    return (@($status).Count -gt 0)
}

function Add-MappedBullet($bulletMap, [string]$key, [string]$text, [string]$file) {
    if ($null -eq $bulletMap) { return }
    if ([string]::IsNullOrWhiteSpace($key)) { return }

    $t = ""
    if ($null -ne $text) { $t = ($text.ToString()).Trim() }
    if ([string]::IsNullOrWhiteSpace($t)) { return }

    if (-not $bulletMap.Contains($key)) {
        $bulletMap[$key] = [ordered]@{
            text  = $t
            files = (New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase))
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($file)) {
        [void]$bulletMap[$key].files.Add($file.Trim())
    }
}

function Normalize-CommitSubject([string]$subject) {
    if ([string]::IsNullOrWhiteSpace($subject)) { return "" }

    $s = $subject.Trim()

    # Normalizacion liviana para que no mezcle tanto ingles/convenciones tecnicas en el instalador.
    # Ej: "fix(app): algo" -> "Correccion: algo"
    $o = [char]0x00F3 # o-acute
    $m = [Regex]::Match($s, '^(?<type>feat|fix|refactor|docs|test|chore|perf|build|ci|style)(\([^)]+\))?:\s*(?<msg>.+)$', 'IgnoreCase')
    if ($m.Success) {
        $type = $m.Groups['type'].Value.ToLowerInvariant()
        $msg = $m.Groups['msg'].Value.Trim()
        switch ($type) {
            'feat'     { return "Nueva funcionalidad: $msg" }
            'fix'      { return "Correcci${o}n: $msg" }
            'refactor' { return "Refactorizaci${o}n: $msg" }
            'docs'     { return "Documentaci${o}n: $msg" }
            'test'     { return "Pruebas: $msg" }
            'chore'    { return "Mantenimiento: $msg" }
            'perf'     { return "Rendimiento: $msg" }
            'build'    { return "Build/paquete: $msg" }
            'ci'       { return "CI: $msg" }
            'style'    { return "Estilo: $msg" }
            default    { return $msg }
        }
    }

    return $s
}

function Is-GenericMessage([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return $true }
    $t = $text.Trim().ToLowerInvariant()
    # "pequeños" sin depender del encoding del .ps1: \x{00F1} = ñ
    if ($t -match '^(cambios?|cambio|cambios nuevos|nuevo(s)? cambios?|un nuevo cambio|peque(\x{00F1}|n)os cambios|cambio user)$') { return $true }
    if ($t.Length -lt 8) { return $true }
    return $false
}

if (-not (Test-Path -LiteralPath $IssPath)) {
    throw "No se encontro el .iss: $IssPath"
}

function Find-GitRoot([string]$startPath) {
    if ([string]::IsNullOrWhiteSpace($startPath)) { return $null }
    try {
        $current = Resolve-Path -LiteralPath $startPath
    } catch {
        return $null
    }

    $dir = New-Object System.IO.DirectoryInfo($current.Path)
    while ($dir -ne $null) {
        $gitDir = Join-Path $dir.FullName ".git"
        if (Test-Path -LiteralPath $gitDir) { return $dir.FullName }
        $dir = $dir.Parent
    }
    return $null
}

$repoRoot = Find-GitRoot (Split-Path -Parent $IssPath)
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    throw "No se encontró carpeta .git. Compile el .iss desde el repo local (no desde la carpeta de red) para poder generar release notes reales."
}

Push-Location $repoRoot
try {
    $issRel = Resolve-Path $IssPath | ForEach-Object {
        $_.Path.Substring($repoRoot.Length).TrimStart('\')
    }
    $issRelGit = ($issRel -replace '\\','/')

    $currentIss = Get-Content -LiteralPath $IssPath -Raw
    $currentVersion = $null
    if (-not [string]::IsNullOrWhiteSpace($AppVersion)) {
        $currentVersion = $AppVersion.Trim()
    }
    else {
        $currentVersion = Get-AppVersionFromIss $currentIss
    }
    if (-not $currentVersion) { throw "No pude leer AppVersion (use -AppVersion o defina AppVersion= en $IssPath)" }

    $headCommitLine = (Invoke-Git @('rev-parse','HEAD') | Select-Object -First 1)
    $headCommit = ""
    if ($null -ne $headCommitLine) { $headCommit = $headCommitLine.ToString().Trim() }
    $state = Load-State $StateFile
    $baseCommit = $null
    if ($state -and $state.lastCommit) {
        $baseCommit = ($state.lastCommit.ToString()).Trim()
    }

    # Buscar la version previa en el historial del .iss (primer commit hacia atras con AppVersion distinta).
    $prevVersion = $null
    $prevCommit = $null
    $commits = Invoke-Git @('log','-n','40','--pretty=format:%H','--',"$issRelGit")
    foreach ($c in $commits) {
        $oldIss = $null
        try {
            $oldIss = (Invoke-Git @('show',"$c`:$issRelGit") | Out-String)
        }
        catch {
            continue
        }
        if (-not $oldIss) { continue }
        $v = Get-AppVersionFromIss $oldIss
        if ($v -and ($v -ne $currentVersion)) {
            $prevVersion = $v
            $prevCommit = $c
            break
        }
    }

    # PowerShell 5 puede interpretar mal acentos si el .ps1 esta sin BOM.
    # Construimos algunos caracteres por codigo para asegurar ortografia.
    $o = [char]0x00F3 # o-acute
    $a = [char]0x00E1 # a-acute
    $e = [char]0x00E9 # e-acute
    $i = [char]0x00ED # i-acute
    $u = [char]0x00FA # u-acute
    $ntilde = [char]0x00F1 # n-tilde
    $header = "Cambios incluidos en esta versi${o}n ($currentVersion):"
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add($header)
    $lines.Add("")

    # 1) Generación "profesional": bullets generales basados en archivos cambiados desde la última compilación del setup.
    $repoChangedFiles = @(Get-ChangedFiles $baseCommit $headCommit 40)
    $workingTreeFilesSet = Get-WorkingTreeChangedFiles
    $workingTreeFiles = @($workingTreeFilesSet)

    $changedFiles = @(
        $repoChangedFiles +
        $workingTreeFiles |
        Where-Object { $_ -ne $null } |
        ForEach-Object { $_.ToString() } |
        Sort-Object -Unique
    )
    $changedFilesCount = @($changedFiles).Count
    $bulletMap = [ordered]@{}

    # Cambios puntuales (por archivo), con reglas de mapeo a descripciones humanas.
    $changed = $changedFiles | ForEach-Object { $_.ToString().Replace('\\','/') }

    # Evitamos ruido (bin/obj/.vs) pero NO filtramos por carpeta, para que cualquier cambio real
    # se refleje en release notes (tal como pidió el usuario).
    $relevantChanged = @(
        $changed | Where-Object {
            ($_ -notmatch '(^|/)(bin|obj|\\.vs)(/|$)') -and
            ($_ -notmatch '(^|/)\\.vscode(/|$)') -and
            ($_ -notmatch '(^|/)_tmp_translate(/|$)')
        }
    )

    foreach ($file in $relevantChanged) {
        $f = ""
        if ($null -ne $file) { $f = $file.ToString().Trim() }
        if ([string]::IsNullOrWhiteSpace($f)) { continue }

        # Ignoramos ruido.
        if ($f -match '/\\.vscode/' -or $f -match '/_tmp_translate/' -or $f -match '^\\.vscode/') { continue }

        # Normalizamos para matchear reglas sin el prefijo del proyecto.
        $rel = $f.Replace('SistemaDeInstalacion/','')
        if ($rel -match '^\\.vscode/' -or $rel -match '^_tmp_translate/') { continue }

        # Reglas puntuales (agregá más si querés mayor detalle).
        if ($rel -match '^ViewModels/AdminLoginViewModel\.cs$') {
            Add-MappedBullet $bulletMap "admin-login" "Login administrativo: developer deshabilitado entra como Administrador." $rel
            continue
        }
        if ($rel -match '^Views/DeveloperAccountsView\.xaml$' -or $rel -match '^ViewModels/DeveloperAccountsViewModel\.cs$' -or $rel -match '^Db/DeveloperAccountsDbService\.cs$') {
            Add-MappedBullet $bulletMap "superadmin-developers" "Super Admin: gesti${o}n de Developers (habilitar/deshabilitar)." $rel
            continue
        }
        if ($rel -match '^Services/PrivilegedProfile\.cs$' -or $rel -match '^Services/SuperAdminPolicy\.cs$' -or $rel -match '^Services/SesionUsuario\.cs$') {
            Add-MappedBullet $bulletMap "permissions-profiles" "Permisos: ajustes de perfiles (Usuario/Admin/Developer/Super Admin)." $rel
            continue
        }
        if ($rel -match '^Services/DocumentationService\.cs$' -or $rel -match '^ViewModels/HelpViewModel\.cs$') {
            Add-MappedBullet $bulletMap "docs-profile" "Ayuda/Documentaci${o}n: filtrado seg${u}n perfil (Admin vs Developer)." $rel
            continue
        }
        if ($rel -match '^Services/LocalizationService\.cs$' -or $rel -match '^Properties/Resources(\..+)?\.resx$') {
            Add-MappedBullet $bulletMap "i18n" "Traducci${o}n: mejoras de i18n (ES/EN/PT-BR) y ortograf${i}a." $rel
            continue
        }
        if ($rel -match '^Views/LogsView\.xaml$' -or $rel -match '^ViewModels/LogsViewModel\.cs$' -or $rel -match '^Services/LogDashboardService\.cs$' -or $rel -match '^Services/LogService\.cs$') {
            Add-MappedBullet $bulletMap "logs-metrics" "Logs/M${e}tricas: mejoras en dashboard y registro de eventos." $rel
            continue
        }
        if ($rel -match '^Deploy/SetupSistema\.iss$') {
            Add-MappedBullet $bulletMap "installer-ui" "Instalador: actualizaci${o}n de flujo y pantalla de notas de versi${o}n." $rel
            continue
        }
        if ($rel -match '^Deploy/GenerateReleaseNotes\.ps1$') {
            Add-MappedBullet $bulletMap "installer-notes" "Instalador: generaci${o}n autom${a}tica de notas de versi${o}n." $rel
            continue
        }
        if ($rel -match '^LauncherSistema/LauncherService\.cs$') {
            Add-MappedBullet $bulletMap "launcher-version" "Launcher: mejoras en detecci${o}n/visualizaci${o}n de versi${o}n instalada." $rel
            continue
        }
        if ($rel -match '^MainWindow\.xaml$' -or $rel -match '^ViewModels/MainViewModel\.cs$' -or $rel -match '^Services/GitBranchService\.cs$') {
            Add-MappedBullet $bulletMap "ui-branch" "UI: footer muestra la rama actual (ProgramTranslation, etc.)." $rel
            continue
        }

        # Fallback: agrupamos como "Otros" para no spamear con una lista gigante de archivos.
        Add-MappedBullet $bulletMap "others" "Otros: ajustes generales." $rel
    }

    # Si no hubo cambios desde la última compilación del instalador, lo informamos y listo.
    if ($changedFilesCount -eq 0 -and -not [string]::IsNullOrWhiteSpace($baseCommit) -and ($baseCommit -eq $headCommit) -and -not (Is-WorkingTreeDirty)) {
        Add-MappedBullet $bulletMap "no-changes" "Sin cambios detectados desde la ${u}ltima compilaci${o}n del instalador (repo limpio)." ""
    }

    # 2) Si no pudimos inferir nada, caemos a commits recientes acotados (filtrados y sin duplicados).
    if ($bulletMap.Count -eq 0) {
        $range = "HEAD"
        if (-not [string]::IsNullOrWhiteSpace($baseCommit) -and (Is-Ancestor $baseCommit $headCommit) -and ($baseCommit -ne $headCommit)) {
            $range = "$baseCommit..$headCommit"
        }

        $subjects = Invoke-Git @('log',$range,'--no-merges','--pretty=format:%s','-n',([Math]::Min($MaxCommits,30)).ToString())
        foreach ($s in $subjects) {
            $t = ""
            if ($null -ne $s) { $t = ($s.ToString()).Trim() }
            if ([string]::IsNullOrWhiteSpace($t)) { continue }
            $nt = Normalize-CommitSubject $t
            if ([string]::IsNullOrWhiteSpace($nt)) { continue }
            if (Is-GenericMessage $nt) { continue }
            Add-MappedBullet $bulletMap ("commit:" + $nt.ToLowerInvariant()) $nt ""
            if ($bulletMap.Count -ge 10) { break }
        }
    }

    # Límite: máximo 10 bullets.
    # Serializamos bullets (máximo 10) y agregamos archivos relevantes por bullet.
    $bulletIndex = 0
    foreach ($k in $bulletMap.Keys) {
        $bulletIndex++
        if ($bulletIndex -gt 10) { break }

        $entry = $bulletMap[$k]
        $text = $entry.text
        $files = @($entry.files)

        if ($files.Count -gt 0) {
            $short = @($files | Sort-Object | Select-Object -First 4)
            $text += " (Archivos: " + ($short -join ", ")
            if ($files.Count -gt $short.Count) { $text += ", ..." }
            $text += ")"
        }

        $lines.Add("- " + $text)
    }

    if ($bulletMap.Count -eq 0) {
        $lines.Add("- (Sin cambios registrados en commits desde la versi${o}n anterior.)")
        if ($prevVersion) {
            $lines.Add("  Versi${o}n anterior detectada: $prevVersion")
        }
    }

    # Inno Setup (Unicode) detecta UTF-8 si el archivo tiene BOM.
    # Escribimos UTF-8 con BOM para evitar textos raros por encoding en cualquier PC.
    $content = ($lines -join "`r`n") + "`r`n"
    $dir = Split-Path -Parent $OutFile
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($OutFile, $content, $utf8Bom)

    # Opcional: actualizar version.txt en el share del instalador para que el Launcher
    # siempre muestre una versión corta (semver) y no el build largo.
    if (-not [string]::IsNullOrWhiteSpace($ServerVersionFile)) {
        $svPath = $ServerVersionFile.Trim()
        try {
            $svDir = Split-Path -Parent $svPath
            if (-not [string]::IsNullOrWhiteSpace($svDir) -and -not (Test-Path -LiteralPath $svDir)) {
                New-Item -ItemType Directory -Path $svDir | Out-Null
            }

            # Usamos ASCII/UTF8 sin caracteres especiales; basta 1 línea.
            [System.IO.File]::WriteAllText(
                $svPath,
                ($currentVersion + "`r`n"),
                (New-Object System.Text.UTF8Encoding($false)))
        }
        catch {
            throw "No pude escribir ServerVersionFile: $svPath. Verifique permisos de escritura en el share."
        }
    }

    # Guardamos estado para la próxima compilación del instalador (sirve para acotar cambios).
    Save-State $StateFile $currentVersion $headCommit

    Write-Host "OK. Generado: $OutFile"
}
finally {
    Pop-Location
}
