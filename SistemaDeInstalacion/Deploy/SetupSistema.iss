#define InstallerRoot "\\comde019\DFSMDE\PUBLIC\CO_MDE_DISENO_DI\RESPALDO DISEÑOS\SAP - Respaldo diseños\FORMATOS SAP\InstallerSystem"
#define ReleaseNotesFile "release-notes.txt"
#define ReleaseNotesGenerator "GenerateReleaseNotes.ps1"
#define BumpVersionScript "BumpReleaseVersion.ps1"
#define VersionFileName "release.version.txt"

#define BumpPs1 (SourcePath + "\\" + BumpVersionScript)
#define ReleaseNotesPs1 (SourcePath + "\\" + ReleaseNotesGenerator)
#define ReleaseNotesOut (SourcePath + "\\" + ReleaseNotesFile)
#define IssFullPath (SourcePath + "\\SetupSistema.iss")
#define ServerVersionTxt (InstallerRoot + "\\version.txt")
#define CsprojFullPath (SourcePath + "\\..\\SistemaDeInstalacion.csproj")
#define VersionFilePath (SourcePath + "\\" + VersionFileName)

#define BumpExitCode Exec( \
  "powershell.exe", \
  "-NoProfile -ExecutionPolicy Bypass -File " + """" + BumpPs1 + """" + \
  " -CsprojPath " + """" + CsprojFullPath + """" + \
  " -OutVersionFile " + """" + VersionFilePath + """" , \
  SourcePath, \
  1 \
)

#if BumpExitCode != 0
  #error Version bump failed. Run Deploy\BumpReleaseVersion.ps1 manually to see details.
#endif

#define _vh FileOpen(VersionFilePath)
#define AppVer FileRead(_vh)
#expr FileClose(_vh)

#define ReleaseNotesExitCode Exec( \
  "powershell.exe", \
  "-NoProfile -ExecutionPolicy Bypass -File " + """" + ReleaseNotesPs1 + """" + \
  " -IssPath " + """" + IssFullPath + """" + \
  " -AppVersion " + """" + AppVer + """" + \
  " -OutFile " + """" + ReleaseNotesOut + """" + \
  " -ServerVersionFile " + """" + ServerVersionTxt + """" , \
  SourcePath, \
  1 \
)

#if ReleaseNotesExitCode != 0
  #error Release notes generator failed. Run Deploy\GenerateReleaseNotes.ps1 manually to see details.
#endif

[Setup]
AppName=SistemaDeInstalacion
AppVersion={#AppVer}
DefaultDirName={pf}\SistemaDeInstalacion
DefaultGroupName=SistemaDeInstalacion
OutputDir={#InstallerRoot}
OutputBaseFilename=SetupSistema
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "{#InstallerRoot}\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#InstallerRoot}\LauncherSistema\LauncherSistema.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#InstallerRoot}\LauncherSistema\LauncherSistema.exe.config"; DestDir: "{app}"; Flags: skipifsourcedoesntexist ignoreversion
; Release notes embebidas en el instalador para que el flujo visible no dependa de archivos externos.
Source: "{#SourcePath}\release-notes.txt"; Flags: dontcopy

[Icons]
Name: "{group}\SistemaDeInstalacion"; Filename: "{app}\LauncherSistema.exe"
Name: "{commondesktop}\SistemaDeInstalacion"; Filename: "{app}\LauncherSistema.exe"

[Run]
Filename: "{app}\LauncherSistema.exe"; Parameters: "--post-update"; Description: "Abrir sistema"; WorkingDir: "{app}"; Flags: nowait

[Code]
var
  UpdateInfoTitleLabel: TNewStaticText;
  UpdateInfoMemo: TNewMemo;
  InstallingUiPrepared: Boolean;
  UpdateAnnouncementShown: Boolean;
  OriginalProgressGaugeTop: Integer;
  OriginalStatusLabelTop: Integer;
  OriginalFilenameLabelTop: Integer;
  OriginalStatusLabelCaption: String;
  OriginalFilenameLabelVisible: Boolean;

function IsUpdateFlow(): Boolean;
begin
  Result := CompareText(ExpandConstant('{param:UPDATEFLOW|0}'), '1') = 0;
end;

function ReadTextFileOrDefault(const FileName: String; const DefaultValue: String): String;
var
  LoadedLines: TArrayOfString;
  I: Integer;
begin
  Result := DefaultValue;
  if FileExists(FileName) then
  begin
    if LoadStringsFromFile(FileName, LoadedLines) and (GetArrayLength(LoadedLines) > 0) then
    begin
      Result := '';
      for I := 0 to GetArrayLength(LoadedLines) - 1 do
      begin
        if Result <> '' then
          Result := Result + #13#10;
        Result := Result + LoadedLines[I];
      end;
    end;
  end;

  Result := Trim(Result);
  if Result = '' then
    Result := DefaultValue;
end;

function GetReleaseNotesPath(): String;
begin
  Result := AddBackslash(ExpandConstant('{tmp}')) + 'release-notes.txt';
end;

function GetReleaseNotesText(): String;
begin
  try
    ExtractTemporaryFile('release-notes.txt');
  except
    // Si por alguna razón no está embebido, seguimos con fallback.
  end;

  Result := ReadTextFileOrDefault(
    GetReleaseNotesPath(),
    'No se encontraron release notes para esta versi' + #243 + 'n.' + #13#10#13#10 +
    'La actualizaci' + #243 + 'n continuar' + #225 + ' autom' + #225 + 'ticamente y la aplicaci' + #243 + 'n se volver' + #225 + ' a abrir al finalizar.');
end;

procedure InitializeInstallingUpdateUi();
var
  ContentTop: Integer;
begin
  if InstallingUiPrepared then
    exit;

  OriginalProgressGaugeTop := WizardForm.ProgressGauge.Top;
  OriginalStatusLabelTop := WizardForm.StatusLabel.Top;
  OriginalFilenameLabelTop := WizardForm.FilenameLabel.Top;
  OriginalStatusLabelCaption := WizardForm.StatusLabel.Caption;
  OriginalFilenameLabelVisible := WizardForm.FilenameLabel.Visible;

  UpdateInfoTitleLabel := TNewStaticText.Create(WizardForm);
  UpdateInfoTitleLabel.Parent := WizardForm.InstallingPage;
  UpdateInfoTitleLabel.Left := WizardForm.StatusLabel.Left;
  UpdateInfoTitleLabel.Top := WizardForm.StatusLabel.Top;
  UpdateInfoTitleLabel.Width := WizardForm.ProgressGauge.Width;
  UpdateInfoTitleLabel.Caption := 'Cambios de esta versi' + #243 + 'n';
  UpdateInfoTitleLabel.Font.Style := [fsBold];
  UpdateInfoTitleLabel.Visible := False;

  UpdateInfoMemo := TNewMemo.Create(WizardForm);
  UpdateInfoMemo.Parent := WizardForm.InstallingPage;
  UpdateInfoMemo.Left := UpdateInfoTitleLabel.Left;
  UpdateInfoMemo.Top := UpdateInfoTitleLabel.Top + UpdateInfoTitleLabel.Height + ScaleY(6);
  UpdateInfoMemo.Width := WizardForm.ProgressGauge.Width;
  UpdateInfoMemo.Height := ScaleY(150);
  UpdateInfoMemo.ReadOnly := True;
  UpdateInfoMemo.ScrollBars := ssVertical;
  UpdateInfoMemo.WordWrap := True;
  UpdateInfoMemo.WantReturns := True;
  UpdateInfoMemo.Visible := False;

  ContentTop := UpdateInfoMemo.Top + UpdateInfoMemo.Height + ScaleY(12);
  WizardForm.ProgressGauge.Top := ContentTop;
  WizardForm.StatusLabel.Top := WizardForm.ProgressGauge.Top + WizardForm.ProgressGauge.Height + ScaleY(6);
  WizardForm.FilenameLabel.Top := WizardForm.StatusLabel.Top + WizardForm.StatusLabel.Height + ScaleY(4);

  InstallingUiPrepared := True;
end;

procedure ShowUpdateAnnouncement();
var
  RemainingSeconds: Integer;
begin
  if not IsUpdateFlow() or UpdateAnnouncementShown then
    exit;

  InitializeInstallingUpdateUi();

  UpdateInfoTitleLabel.Visible := True;
  UpdateInfoMemo.Text := GetReleaseNotesText();
  UpdateInfoMemo.Visible := True;
  WizardForm.FilenameLabel.Visible := False;

  for RemainingSeconds := 10 downto 1 do
  begin
    WizardForm.Update;
    Sleep(1000);
  end;

  UpdateAnnouncementShown := True;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpInstalling then
    ShowUpdateAnnouncement();
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    SaveStringToFile(
      ExpandConstant('{app}\build.version'),
      '{#SetupSetting("AppVersion")}',
      False);
  end;
end;
