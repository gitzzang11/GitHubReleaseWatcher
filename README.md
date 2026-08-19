# GitHub Release Watcher

Windows에서 GitHub 저장소의 새 Release를 주기적으로 확인하고 네이티브 알림으로 알려 주는 가벼운 WPF 앱입니다.

## 사용법

1. `GitHubReleaseWatcher.exe`를 실행합니다.
2. `https://github.com/owner/repository` 형태의 URL을 붙여넣고 **추가**를 누릅니다.
3. 현재 Release가 기준 버전으로 저장되며, 이후 새 Release부터 알림이 표시됩니다.
4. 창의 X 버튼은 기본적으로 트레이로 최소화합니다. 실제 종료는 트레이 메뉴의 **종료**를 사용합니다.

설정에서는 자동 확인 간격, Pre-release 포함, Windows 시작 시 실행, GitHub Personal Access Token을 구성할 수 있습니다. Token은 JSON이 아닌 Windows 자격 증명 관리자에 저장됩니다.

앱은 실행 직후 한 번 Release를 확인합니다. Windows가 잠겨 있으면 새 Release 알림을 앱 안에 보류하고,
사용자가 잠금을 해제해 바탕화면으로 돌아온 뒤에 표시합니다.

## 개발 명령

```powershell
dotnet restore GitHubReleaseWatcher.slnx
dotnet build GitHubReleaseWatcher.slnx -c Debug --no-restore
dotnet test GitHubReleaseWatcher.slnx -c Debug --no-build --no-restore
dotnet publish src\GitHubReleaseWatcher\GitHubReleaseWatcher.csproj -c Release -r win-x64 --self-contained true
```

설치 프로그램을 만들려면 Inno Setup 6가 설치된 환경에서 다음을 실행합니다.

```powershell
.\installer\build-installer.ps1
```

설치 프로그램은 고정 AppId로 기존 설치를 감지합니다. 낮은 버전은 같은 설치 경로에서 업데이트하고,
같은 버전은 복구 설치하며, 이미 설치된 더 높은 버전으로의 다운그레이드는 차단합니다.
앱 설정과 저장소 목록은 설치 폴더 밖의 `%LOCALAPPDATA%\GitHubReleaseWatcher`에 유지됩니다.

사용자 데이터와 로그는 `%LOCALAPPDATA%\GitHubReleaseWatcher`에 저장됩니다.
