; Inno Setup Script for Frisson
; This script creates an installer that automatically adds a Windows Firewall
; inbound rule during installation and removes it during uninstallation.

#define MyAppVersion GetFileVersion("src\Frisson.App\bin\Release\net10.0\Frisson.App.exe")

[Setup]
AppName=Frisson
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\Frisson
OutputDir=installer
OutputBaseFilename=Frisson-Setup
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
Source: "src\Frisson.App\bin\Release\net10.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Run]
; Add Windows Firewall inbound rule for Frisson on install
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Frisson"" dir=in action=allow program=""{app}\Frisson.App.exe"" enable=yes"; Flags: runhidden;

[UninstallRun]
; Remove Windows Firewall rule for Frisson on uninstall
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Frisson"""; Flags: runhidden;

[Icons]
Name: "{group}\Frisson"; Filename: "{app}\Frisson.App.exe"
Name: "{autodesktop}\Frisson"; Filename: "{app}\Frisson.App.exe"
