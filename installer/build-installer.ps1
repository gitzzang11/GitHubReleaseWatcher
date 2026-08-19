[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path $workspaceRoot 'artifacts\publish\win-x64'
$installerOutputDirectory = Join-Path $workspaceRoot 'artifacts\installer'
$projectFile = Join-Path $workspaceRoot 'src\GitHubReleaseWatcher\GitHubReleaseWatcher.csproj'
$installerScript = Join-Path $PSScriptRoot 'GitHubReleaseWatcher.iss'

$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw 'Inno Setup 6 컴파일러를 찾을 수 없습니다. winget install --id JRSoftware.InnoSetup -e 로 설치해 주세요.'
}

if (Test-Path -LiteralPath $publishDirectory) {
    $resolvedPublishDirectory = (Resolve-Path -LiteralPath $publishDirectory).Path
    $expectedParent = (Join-Path $workspaceRoot 'artifacts\publish') + '\'
    if (-not $resolvedPublishDirectory.StartsWith($expectedParent, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "예상하지 못한 publish 경로입니다: $resolvedPublishDirectory"
    }
    Remove-Item -LiteralPath $resolvedPublishDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $installerOutputDirectory -Force | Out-Null

& dotnet publish $projectFile -c Release -r win-x64 --self-contained true --no-restore -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Release publish가 실패했습니다.' }

& $compiler /Qp $installerScript
if ($LASTEXITCODE -ne 0) { throw '설치 프로그램 컴파일이 실패했습니다.' }

$installer = Get-ChildItem -LiteralPath $installerOutputDirectory -Filter 'GitHubReleaseWatcher-Setup-*.exe' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $installer) { throw '생성된 설치 프로그램을 찾을 수 없습니다.' }

Write-Host "설치 프로그램: $($installer.FullName)"
Write-Host "크기: $([math]::Round($installer.Length / 1MB, 2)) MB"
