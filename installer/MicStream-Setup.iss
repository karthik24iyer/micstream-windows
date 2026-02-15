; MicStream Receiver - Bundled Installer with VB-Cable
; Creates professional installation experience
; Includes VB-Cable virtual audio driver + MicStream receiver

#define MyAppName "MicStream Receiver"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Karthik"
#define MyAppURL "https://github.com/karthik/micstream"
#define MyAppExeName "MicStreamReceiver.exe"

[Setup]
; Basic Information
AppId={{8F9A3B2C-1D4E-5F6A-7B8C-9D0E1F2A3B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\MicStream
DefaultGroupName=MicStream
DisableProgramGroupPage=yes
;LicenseFile=..\LICENSE.txt
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\output
OutputBaseFilename=MicStream-Receiver-Setup-v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Icons and Images
;SetupIconFile=..\assets\icon.ico
WizardImageFile=assets\wizard-image.bmp
WizardSmallImageFile=assets\wizard-image.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "installvbcable"; Description: "Install VB-Cable virtual audio driver (Required for virtual microphone)"; GroupDescription: "Virtual Audio Driver:"; Flags: checkedonce

[Files]
; Main application
Source: "sources\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Documentation
Source: "sources\README.md"; DestDir: "{app}"; Flags: ignoreversion
;Source: "..\PHASE2_SUMMARY.md"; DestDir: "{app}"; Flags: ignoreversion
;Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
; VB-Cable installer and ALL driver files (CRITICAL: All files needed for driver installation)
Source: "vbcable\*"; DestDir: "{tmp}\vbcable"; Flags: deleteafterinstall recursesubdirs; Tasks: installvbcable
; Helper scripts
Source: "scripts\verify-installation.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "scripts\uninstall-vbcable.bat"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Verify Installation"; Filename: "{app}\verify-installation.bat"
Name: "{group}\Uninstall VB-Cable"; Filename: "{app}\uninstall-vbcable.bat"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; Install VB-Cable driver (run from extracted directory with all files)
Filename: "{tmp}\vbcable\VBCABLE_Setup_x64.exe"; StatusMsg: "Installing VB-Cable virtual audio driver..."; Flags: waituntilterminated runascurrentuser; Tasks: installvbcable; Check: VBCableNotInstalled
; Show completion message
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if VB-Cable is already installed
function VBCableNotInstalled: Boolean;
var
  UninstallKey: String;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\VB-Audio Virtual Cable';
  Result := not RegKeyExists(HKEY_LOCAL_MACHINE, UninstallKey);
end;

// Custom messages during installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('Installation Complete!' + #13#10 + #13#10 +
           'MicStream Receiver has been installed.' + #13#10 + #13#10 +
           'Next Steps:' + #13#10 +
           '1. Restart your PC (required for VB-Cable)' + #13#10 +
           '2. Run MicStream Receiver' + #13#10 +
           '3. Press [S] to start listening' + #13#10 +
           '4. Open Discord → Input Device → "CABLE Output"' + #13#10 +
           '5. Connect from Android MicStream app' + #13#10 + #13#10 +
           'Enjoy your wireless microphone!',
           mbInformation, MB_OK);
  end;
end;

// Custom page for setup instructions
procedure InitializeWizard();
var
  InfoPage: TOutputMsgWizardPage;
begin
  InfoPage := CreateOutputMsgPage(wpWelcome,
    'About MicStream Receiver',
    'Wireless Microphone for Gaming',
    'MicStream turns your Android phone into a wireless microphone for Discord, CS:GO, Valorant, and more.' + #13#10 + #13#10 +
    'Features:' + #13#10 +
    '• Low-latency audio streaming (<200ms)' + #13#10 +
    '• Auto-discovery via mDNS' + #13#10 +
    '• Virtual microphone (VB-Cable)' + #13#10 +
    '• Works with Discord, games, OBS' + #13#10 + #13#10 +
    'This installer will:' + #13#10 +
    '1. Install MicStream Receiver' + #13#10 +
    '2. Install VB-Cable virtual audio driver' + #13#10 +
    '3. Configure everything automatically' + #13#10 + #13#10 +
    'Requirements:' + #13#10 +
    '• Windows 10/11 (64-bit)' + #13#10 +
    '• Android phone with MicStream app' + #13#10 +
    '• Same WiFi network');
end;
