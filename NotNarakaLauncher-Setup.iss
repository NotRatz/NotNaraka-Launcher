; NotNaraka Launcher - Inno Setup Script
; Download Inno Setup from: https://jrsoftware.org/isinfo.php
; Then compile this script to create a professional Windows installer

[Setup]
AppName=NotNaraka Launcher
AppVersion=1.0.0
AppPublisher=Ratz
AppPublisherURL=https://twitch.tv/YourChannel
AppSupportURL=https://twitch.tv/YourChannel
AppUpdatesURL=https://twitch.tv/YourChannel
DefaultDirName={autopf}\NotNaraka Launcher
DefaultGroupName=NotNaraka Launcher
AllowNoIcons=yes
LicenseFile=
OutputDir=.
OutputBaseFilename=NotNaraka-Launcher-Setup
SetupIconFile=Assets\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "NarakaTweaks.Launcher\bin\Release\net8.0-windows\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs; Attribs: readonly

[Icons]
Name: "{group}\NotNaraka Launcher"; Filename: "{app}\NarakaTweaks.Launcher.exe"
Name: "{group}\{cm:UninstallProgram,NotNaraka Launcher}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\NotNaraka Launcher"; Filename: "{app}\NarakaTweaks.Launcher.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\NotNaraka Launcher"; Filename: "{app}\NarakaTweaks.Launcher.exe"; Tasks: quicklaunchicon

[Run]
Filename: "https://dotnet.microsoft.com/download/dotnet/8.0/runtime"; Description: "Download .NET 8.0 Runtime (Required)"; Flags: postinstall shellexec skipifsilent nowait; Check: not IsDotNetInstalled
Filename: "{app}\NarakaTweaks.Launcher.exe"; Description: "{cm:LaunchProgram,NotNaraka Launcher}"; Flags: postinstall shellexec skipifsilent nowait; Check: IsDotNetInstalled

[Code]
function IsDotNetInstalled: Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
begin
  Result := False;
  if Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
      Result := True;
  end;
end;

procedure InitializeWizard;
begin
  if not IsDotNetInstalled then
  begin
    MsgBox('.NET 8.0 Desktop Runtime is required to run this application.' + #13#10 + #13#10 + 
           'The installer will offer to open the download page after installation.', 
           mbInformation, MB_OK);
  end;
end;
