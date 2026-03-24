#define InstallerRoot "\\comde019\DFSMDE\PUBLIC\CO_MDE_DISENO_DI\RESPALDO DISEÑOS\SAP - Respaldo diseños\FORMATOS SAP\InstallerSystem"

[Setup]
AppName=SistemaDeInstalacion
AppVersion=1.0.0.6
DefaultDirName={pf}\SistemaDeInstalacion
DefaultGroupName=SistemaDeInstalacion
OutputDir={#InstallerRoot}
OutputBaseFilename=SetupSistema
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "{#InstallerRoot}\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "{#InstallerRoot}\LauncherSistema\LauncherSistema.exe"; DestDir: "{app}"
Source: "{#InstallerRoot}\LauncherSistema\LauncherSistema.exe.config"; DestDir: "{app}"; Flags: skipifsourcedoesntexist

[Icons]
Name: "{group}\SistemaDeInstalacion"; Filename: "{app}\LauncherSistema.exe"
Name: "{commondesktop}\SistemaDeInstalacion"; Filename: "{app}\LauncherSistema.exe"

[Run]
Filename: "{app}\LauncherSistema.exe"; Parameters: "--post-update"; Description: "Abrir sistema"; WorkingDir: "{app}"; Flags: nowait postinstall
