#define MyAppName "Code Summarizer"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "sekacorn"
#define MyAppExeName "CodeSummarizer.exe"
#ifndef SourceDir
  #define SourceDir "artifacts\win-x64-restricted"
#endif
#ifndef ProfileName
  #define ProfileName "restricted"
#endif

[Setup]
AppId={{A5369FF9-1E08-4D17-84E8-E3C6DFD5B107}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\CodeSummarizer
DefaultGroupName={#MyAppName}
OutputDir=artifacts
OutputBaseFilename=CodeSummarizer-Windows-{#ProfileName}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
