; Inno Setup Script for Frisson
; This script creates an installer that automatically adds a Windows Firewall
; inbound rule during installation and removes it during uninstallation.
;
; AppId - generated once, never change, identifies Frisson as the same
; application across all build variants and versions.
#define FrissonAppId "{{0A85408A-3262-4F58-B11E-D171CA726D20}"
#define FrissonPublisher "curtainsmall"
#define FrissonPublisherURL "https://github.com/curtainsmall/frisson"
#define FrissonSupportURL "https://github.com/curtainsmall/frisson/issues"
#define FrissonUpdatesURL "https://github.com/curtainsmall/frisson/releases"
;
; Build variants (override on command line):
;   ISCC setup.iss                            -> framework-dependent (default)
;   ISCC /DBuildKind=selfcontained setup.iss  -> self-contained (bundles .NET runtime)

#ifndef BuildKind
  #define BuildKind "framework"
#endif

#if BuildKind == "selfcontained"
  #define SourceDir "src\Frisson.Desktop\publish\win-x64-selfcontained"
  #define AppSuffix " (Self-Contained)"
  #define OutputSuffix "-SelfContained"
#else
  #define SourceDir "src\Frisson.Desktop\publish\win-x64-framework"
  #define AppSuffix ""
  #define OutputSuffix ""
#endif

#define FrissonVersion GetVersionNumbersString(SourceDir + "\Frisson.Desktop.exe")
#define OutputName "Frisson-Setup-" + FrissonVersion + OutputSuffix

[Setup]
AppName=Frisson{#AppSuffix}
AppVersion={#FrissonVersion}
DefaultDirName={autopf}\Frisson{#AppSuffix}
OutputDir=installer
OutputBaseFilename={#OutputName}
AppId={#FrissonAppId}
AppPublisher={#FrissonPublisher}
AppPublisherURL={#FrissonPublisherURL}
AppSupportURL={#FrissonSupportURL}
AppUpdatesURL={#FrissonUpdatesURL}
LicenseFile=LICENSE
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Frisson.Desktop.exe
UsedUserAreasWarning=no
DisableDirPage=auto
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "zh_CN"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "zh_TW"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"

[InstallDelete]
; Clean old files before installing new version to prevent residue
Type: filesandordirs; Name: "{app}\*"
; Clean old Start Menu shortcuts from previous install location
Type: files; Name: "{autoprograms}\(Default)\Frisson.lnk"
Type: files; Name: "{group}\Frisson.lnk"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Run]
; Clean old start menu shortcuts from previous installs
Filename: "powershell.exe"; Parameters: "-NoProfile -Command Remove-Item -Force -ErrorAction SilentlyContinue ([Environment]::GetFolderPath('CommonPrograms') + '\(Default)\Frisson.lnk'), ([Environment]::GetFolderPath('CommonPrograms') + '\Frisson\Frisson.lnk')"; Flags: runhidden
; Add Windows Firewall inbound rule for Frisson on install
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Frisson"" dir=in action=allow program=""{app}\Frisson.Desktop.exe"" enable=yes"; Flags: runhidden;

[UninstallRun]
; Remove Windows Firewall rule for Frisson on uninstall
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Frisson"""; Flags: runhidden; RunOnceId: "RemoveFrissonFirewallRule"

[Icons]
Name: "{autoprograms}\Frisson"; Filename: "{app}\Frisson.Desktop.exe"
Name: "{autodesktop}\Frisson"; Filename: "{app}\Frisson.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Registry]
; Save installer language choice for Frisson to read as default language
Root: HKCU; Subkey: "Software\Frisson"; ValueType: string; ValueName: "Language"; ValueData: "{language}"
