#define AppName "GitHub Release Watcher"
#define AppPublisher "GitHub Release Watcher"
#define AppId "GitHubReleaseWatcher.A0D24D99-36D5-4D1A-82BE-018F67B92C3D"
#define AppExeName "GitHubReleaseWatcher.exe"
#define AppExeSource "..\artifacts\publish\win-x64\GitHubReleaseWatcher.exe"
#define AppVersion GetVersionNumbersString(AppExeSource)

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/
AppSupportURL=https://github.com/
DefaultDirName={localappdata}\Programs\GitHub Release Watcher
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableWelcomePage=no
AllowNoIcons=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=GitHubReleaseWatcher-Setup-{#AppVersion}
SetupIconFile=..\src\GitHubReleaseWatcher\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes
CloseApplications=yes
CloseApplicationsFilter={#AppExeName}
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} 설치 프로그램
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoCopyright=Copyright (c) 2026 {#AppPublisher}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로가기 만들기"; GroupDescription: "추가 바로가기:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{#AppName} 실행"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppId}_is1';

var
  ExistingInstall: Boolean;
  InstalledVersion: String;

function FindExistingInstall(var Version: String): Boolean;
begin
  Result := RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Version);
end;

function InitializeSetup: Boolean;
var
  CurrentPackedVersion: Int64;
  InstalledPackedVersion: Int64;
begin
  Result := True;
  ExistingInstall := FindExistingInstall(InstalledVersion);

  if ExistingInstall and
     StrToVersion(InstalledVersion, InstalledPackedVersion) and
     StrToVersion('{#AppVersion}', CurrentPackedVersion) and
     (ComparePackedVersion(InstalledPackedVersion, CurrentPackedVersion) > 0) then
  begin
    SuppressibleMsgBox(
      '더 최신 버전(' + InstalledVersion + ')이 이미 설치되어 있습니다.' + #13#10 +
      '다운그레이드를 방지하기 위해 설치를 중단합니다.',
      mbError, MB_OK, IDOK);
    Result := False;
  end;
end;

procedure InitializeWizard;
begin
  if ExistingInstall then
  begin
    if InstalledVersion = '{#AppVersion}' then
    begin
      WizardForm.WelcomeLabel1.Caption := '{#AppName} 복구 설치';
      WizardForm.WelcomeLabel2.Caption :=
        '같은 버전(' + InstalledVersion + ')이 이미 설치되어 있습니다.' + #13#10 +
        '다음을 눌러 앱 파일을 복구하거나 다시 설치할 수 있습니다.';
    end
    else
    begin
      WizardForm.WelcomeLabel1.Caption := '{#AppName} 업데이트';
      WizardForm.WelcomeLabel2.Caption :=
        '설치된 ' + InstalledVersion + ' 버전을 {#AppVersion} 버전으로 업데이트합니다.' + #13#10 +
        '설정과 등록한 저장소 목록은 그대로 유지됩니다.';
    end;
  end;
end;
