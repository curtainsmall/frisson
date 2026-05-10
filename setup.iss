; Inno Setup Script for CoyoteStudio
; This script creates an installer that automatically adds a Windows Firewall
; inbound rule during installation and removes it during uninstallation.

#define MyAppVersion GetFileVersion("src\CoyoteStudio.App\bin\Release\net10.0\CoyoteStudio.App.exe")

[Setup]
AppName=CoyoteStudio
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\CoyoteStudio
OutputDir=installer
OutputBaseFilename=CoyoteStudio-Setup
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\CoyoteStudio.App.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
; Clean old files before installing new version to prevent residue
Type: filesandordirs; Name: "{app}\*"

[Files]
Source: "src\CoyoteStudio.App\bin\Release\net10.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Run]
; Add Windows Firewall inbound rule for CoyoteStudio on install
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""CoyoteStudio"" dir=in action=allow program=""{app}\CoyoteStudio.App.exe"" enable=yes"; Flags: runhidden;

[UninstallRun]
; Remove Windows Firewall rule for CoyoteStudio on uninstall
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""CoyoteStudio"""; Flags: runhidden;

[Icons]
Name: "{group}\CoyoteStudio"; Filename: "{app}\CoyoteStudio.App.exe"
Name: "{autodesktop}\CoyoteStudio"; Filename: "{app}\CoyoteStudio.App.exe"
