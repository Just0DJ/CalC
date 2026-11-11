; Production Inno Setup Script for TitanCalc
; Generates single standalone installer executable with NGEN optimization

#define MyAppName "TitanCalc"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Titan Systems Architecture"
#define MyAppExeName "TitanCalc.exe"

[Setup]
AppId={{5B8C248A-8519-4C92-9426-3C6BD7F7A250}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\..\bin
OutputBaseFilename=TitanCalc_Setup_v1.0.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\..\bin\TitanCalc.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Run NGEN Native Image Generator for sub-100ms launch
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\ngen.exe"; Parameters: "install ""{app}\{#MyAppExeName}"" /nologo /silent"; Flags: runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{win}\Microsoft.NET\Framework64\v4.0.30319\ngen.exe"; Parameters: "uninstall ""{app}\{#MyAppExeName}"" /nologo /silent"; Flags: runhidden
