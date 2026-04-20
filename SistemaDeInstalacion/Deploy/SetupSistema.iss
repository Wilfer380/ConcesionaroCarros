#define InstallerRoot "\\comde019\DFSMDE\PUBLIC\CO_MDE_DISENO_DI\RESPALDO DISEÑOS\SAP - Respaldo diseños\FORMATOS SAP\InstallerSystem"

[Setup]
AppName=SistemaDeInstalacion
AppVersion=1.0.0.8
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
  Result := AddBackslash(ExtractFileDir(ExpandConstant('{srcexe}'))) + 'release-notes.txt';
end;

function GetReleaseNotesText(): String;
begin
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
