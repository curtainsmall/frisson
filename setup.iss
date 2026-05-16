; Inno Setup Script for Frisson
; This script creates an installer that automatically adds a Windows Firewall
; inbound rule during installation and removes it during uninstallation.
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

#define MyAppVersion GetFileVersion(SourceDir + "\Frisson.App.exe")

[Setup]
AppName=Frisson{#AppSuffix}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\Frisson{#AppSuffix}
OutputDir=installer
OutputBaseFilename={#OutputName}
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\Frisson.App.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

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
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Frisson"""; Flags: runhidden;

[Icons]
Name: "{group}\Frisson"; Filename: "{app}\Frisson.App.exe"
Name: "{autodesktop}\Frisson"; Filename: "{app}\Frisson.App.exe"
