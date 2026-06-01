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
  #define SourceDir "src\Frisson.App\publish\win-x64-selfcontained"
  #define OutputName "Frisson-Setup-SelfContained"
  #define AppSuffix " (Self-Contained)"
#else
  #define SourceDir "src\Frisson.App\publish\win-x64-framework"
  #define OutputName "Frisson-Setup"
  #define AppSuffix ""
#endif

#define FrissonVersion GetVersionNumbersString(SourceDir + "\Frisson.App.exe")

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
UninstallDisplayIcon={app}\Frisson.App.exe

[Languages]
Name: "en-US"; MessagesFile: "compiler:Default.isl"
Name: "zh-CN"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "zh-TW"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"
Name: "ja-JP"; MessagesFile: "compiler:Languages\Japanese.isl"

[InstallDelete]
; Clean old files before installing new version to prevent residue
Type: filesandordirs; Name: "{app}\*"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Run]
; Add Windows Firewall inbound rule for Frisson on install
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Frisson"" dir=in action=allow program=""{app}\Frisson.App.exe"" enable=yes"; Flags: runhidden;

[UninstallRun]
; Remove Windows Firewall rule for Frisson on uninstall
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Frisson"""; Flags: runhidden; RunOnceId: "RemoveFrissonFirewallRule"

[Icons]
Name: "{group}\Frisson"; Filename: "{app}\Frisson.App.exe"
Name: "{autodesktop}\Frisson"; Filename: "{app}\Frisson.App.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Registry]
; Save installer language choice for Frisson to read as default language
Root: HKCU; Subkey: "Software\Frisson"; ValueType: string; ValueName: "Language"; ValueData: "{language}"
