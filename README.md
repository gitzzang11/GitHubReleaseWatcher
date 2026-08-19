<div align="center">
  <img src="./src/GitHubReleaseWatcher/Assets/AppIcon.png" width="128" alt="GitHub Release Watcher 아이콘">
  <h1>GitHub Release Watcher</h1>
  <p>관심 있는 GitHub 저장소의 새 Release를 놓치지 않도록<br>Windows 알림으로 알려 주는 데스크톱 앱입니다.</p>

  <p>
    <img src="https://img.shields.io/badge/Windows-10%2B-0078D4?logo=windows11&logoColor=white" alt="Windows 10 이상">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10">
    <img src="https://img.shields.io/badge/UI-WPF-5C2D91" alt="WPF">
  </p>

  <p>
    <a href="https://github.com/gitzzang11/GitHubReleaseWatcher/releases/latest"><strong>최신 버전 다운로드</strong></a>
    ·
    <a href="https://github.com/gitzzang11/GitHubReleaseWatcher/issues">문제 제보</a>
  </p>
</div>

---

## 주요 기능

| 기능 | 설명 |
| --- | --- |
| 여러 저장소 감시 | GitHub 저장소 URL을 등록하고 한 화면에서 상태를 확인합니다. |
| 자동 Release 확인 | 앱 시작 직후 한 번 확인하고, 이후 15분~6시간 간격으로 자동 확인합니다. |
| Windows 네이티브 알림 | 새 Release가 발견되면 알림을 표시하고 Release 페이지로 바로 이동합니다. |
| 잠금 화면 알림 보류 | Windows가 잠겨 있으면 알림을 보내지 않고, 잠금 해제 후 바탕화면에서 표시합니다. |
| Pre-release 지원 | 설정에서 Pre-release 포함 여부를 선택할 수 있습니다. |
| 시작 프로그램 및 트레이 | Windows 로그인 시 백그라운드 실행하고 창을 트레이로 최소화할 수 있습니다. |
| API 요청 최적화 | ETag 조건부 요청으로 불필요한 GitHub API 사용을 줄입니다. |
| 안전한 Token 저장 | GitHub Personal Access Token을 Windows 자격 증명 관리자에 저장합니다. |

## 다운로드 및 설치

1. [최신 Release](https://github.com/gitzzang11/GitHubReleaseWatcher/releases/latest)에서 `GitHubReleaseWatcher-Setup-*.exe`를 다운로드합니다.
2. 설치 프로그램을 실행하고 안내에 따라 설치합니다.
3. 이미 설치되어 있다면 기존 설정과 등록한 저장소를 유지한 채 업데이트됩니다.

> [!NOTE]
> 현재 설치 프로그램에는 코드 서명이 없습니다. 처음 실행할 때 Windows SmartScreen 경고가 나타날 수 있습니다.

### 시스템 요구 사항

- Windows 10 버전 2004(빌드 19041) 이상
- 64비트 Windows
- 인터넷 연결

앱은 .NET 런타임을 포함해 배포되므로 사용자가 .NET을 별도로 설치할 필요가 없습니다.

## 사용 방법

1. 앱을 실행합니다.
2. `https://github.com/owner/repository` 형식의 저장소 URL을 입력합니다.
3. **추가**를 누르면 현재 최신 Release가 기준 버전으로 저장됩니다.
4. 이후 새 Release가 공개되면 Windows 알림을 받습니다.

처음 등록할 때는 기존 Release를 새 업데이트로 알리지 않습니다. 창의 닫기 버튼은 기본적으로 앱을 종료하지 않고 트레이로 최소화하며, 완전히 종료하려면 트레이 메뉴에서 **종료**를 선택합니다.

### 비공개 저장소 및 API 한도

GitHub 인증이 필요하거나 API 요청 한도를 높이고 싶다면 설정에서 Personal Access Token을 입력할 수 있습니다. 비공개 저장소를 확인하려면 해당 저장소를 읽을 수 있는 권한을 Token에 부여해야 합니다.

## 알림 동작

- 앱을 실행하면 주기 타이머를 기다리지 않고 즉시 한 번 확인합니다.
- Windows가 잠긴 상태에서 발견된 알림은 앱 내부에 보류됩니다.
- 사용자가 잠금을 해제하고 바탕화면으로 돌아오면 보류된 알림을 표시합니다.
- 같은 Release에 대한 알림은 반복해서 보내지 않습니다.

## 데이터 및 개인정보

앱 설정, 등록한 저장소, 로그는 설치 폴더가 아닌 다음 위치에 저장됩니다.

```text
%LOCALAPPDATA%\GitHubReleaseWatcher
├── settings.json
├── repositories.json
└── logs\app.log
```

GitHub Token은 JSON 파일에 저장하지 않고 Windows 자격 증명 관리자의 `GitHubReleaseWatcher/GitHubToken` 항목에 보관합니다.

## 개발

### 기술 구성

- C# / .NET 10
- WPF 및 MVVM
- Windows App SDK 알림
- MSTest
- Inno Setup 6

### 빌드 및 테스트

```powershell
git clone https://github.com/gitzzang11/GitHubReleaseWatcher.git
cd GitHubReleaseWatcher

dotnet restore GitHubReleaseWatcher.slnx
dotnet build GitHubReleaseWatcher.slnx -c Debug --no-restore
dotnet test GitHubReleaseWatcher.slnx -c Debug --no-build --no-restore
```

### 설치 프로그램 생성

Inno Setup 6가 설치된 Windows 환경에서 실행합니다.

```powershell
.\installer\build-installer.ps1
```

결과 파일은 `artifacts\installer`에 생성됩니다. 설치 프로그램은 고정 AppId로 기존 설치를 감지하고 업데이트·복구 설치·다운그레이드 차단을 처리합니다.

## 프로젝트 구조

```text
GitHubReleaseWatcher
├── src
│   ├── GitHubReleaseWatcher          # WPF 애플리케이션
│   └── GitHubReleaseWatcher.Core     # 모델, GitHub API, 상태 처리
├── tests
│   └── GitHubReleaseWatcher.Tests    # 단위 및 통합 테스트
└── installer                         # Inno Setup 설치 프로그램
```

## 피드백

버그나 개선 제안은 [GitHub Issues](https://github.com/gitzzang11/GitHubReleaseWatcher/issues)에 남겨 주세요.
