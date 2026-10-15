#define MyAppName "Project Atlas"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Project Atlas Shard"
#define MyAppExeName "AtlasLauncher.exe"
#define StagingDir "C:\UO_Projects\AtlasStaging\ReleaseBuild"

[Setup]
AppId={{A8F4E3B2-9C1D-4E7A-8B5F-2D6C9E1A4B7F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
OutputDir=C:\UO_Projects\AtlasStaging\InstallerOutput
OutputBaseFilename=ProjectAtlasSetup
SetupIconFile=..\assets\ProjectAtlas.ico
UninstallDisplayIcon={app}\ProjectAtlas.ico
UninstallDisplayName={#MyAppName}
Compression=lzma2/fast
SolidCompression=no
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
; Grant full Read/Write/Modify permissions on C:\Program Files\Project Atlas so saved passwords, profiles, and updates never hit UAC blocks
Name: "{app}"; Permissions: users-modify
Name: "{app}\Client"; Permissions: users-modify
Name: "{app}\TazUO"; Permissions: users-modify
Name: "{app}\TazUO-Legacy"; Permissions: users-modify
Name: "{app}\Plugins"; Permissions: users-modify
Name: "{app}\Plugins\Razor\Profiles"; Permissions: users-modify
Name: "{app}\Plugins\Razor\Macros"; Permissions: users-modify
Name: "{app}\Plugins\Razor\Scripts"; Permissions: users-modify
Name: "{app}\Plugins\RazorEnhanced\Profiles"; Permissions: users-modify
Name: "{app}\Plugins\RazorEnhanced\Scripts"; Permissions: users-modify
Name: "{app}\Plugins\RazorEnhanced\Settings"; Permissions: users-modify

[Files]
; Package all ReleaseBuild files EXCEPT admin dev.mode, local saved passwords/settings, logs, and local test profiles
Source: "{#StagingDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "dev.mode,manifest.json,*settings.json,*settings.dev.json,*.pdb,*.log,Client\Data\Profiles\*,TazUO\Data\Profiles\*,TazUO-Legacy\Data\Profiles\*,Plugins\Razor\Profiles\*,Plugins\Razor\Macros\*,Plugins\RazorEnhanced\Profiles\*,Plugins\RazorEnhanced\Settings\*"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\ProjectAtlas.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\ProjectAtlas.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
