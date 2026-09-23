<div align="center">

<img src="src/HTools.App/Assets/toolbox.svg" width="96" alt="H.Tools" />

# H.Tools

[简体中文](README.md) | [English](README.en.md) | [日本語](README.ja.md) | **한국어**

일상 업무와 개발·디버깅에 자주 쓰는 작은 도구들을 하나의 창에 모은 Windows용 경량 데스크톱 도구 모음입니다.

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Avalonia](https://img.shields.io/badge/Avalonia-12-8B44AC)

</div>

## ✨ 기능 목록

### 자주 쓰는 도구

| 도구 | 설명 |
| --- | --- |
| 📋 **멀티 클립보드** | 자주 쓰는 내용(텍스트 / 이미지) 10개를 저장하고 전역 단축키 `Ctrl+1` ~ `Ctrl+0`으로 빠르게 붙여넣기 |
| 🖱 **마우스 효과** | 마우스를 빠르게 흔들면 매화 잔상이 나타납니다. 앱 센터에서 클릭 한 번으로 켜고 끄기 |
| ✂ **스크린샷** | 원하는 영역을 드래그하거나 창 위에 마우스를 올려 클릭으로 선택. 사각형·타원(테두리 / 채우기), 직선, 화살표, 펜, 텍스트, 모자이크 주석 지원. 복사, PNG 저장, 화면에 고정 가능. 여러 모니터와 서로 다른 DPI 배율 지원 |
| 📊 **시스템 모니터** | CPU·메모리·네트워크 실시간 그래프, CPU / GPU 온도, 프로세서·메인보드·그래픽·메모리·저장장치·네트워크 어댑터 정보 확인 |

### 개발 도구

| 도구 | 설명 |
| --- | --- |
| 📮 **Mock 클라이언트** | Postman 스타일의 요청 작업 공간. HTTP 요청을 직접 구성하고 응답 확인. 최근 100개 기록 보관 |
| **Mock 서버** | 고정된 응답을 반환하는 서버를 실행하고 받은 모든 요청을 기록 |
| **Mock API 서버** | HTTP 메서드 + 경로로 규칙을 정의해 상태 코드·Content-Type·본문을 반환 |
| **정적 파일 서버** | 로컬 폴더를 HTTP로 공유 |
| **파일 업로드 서버** | 업로드용 웹 폼 제공(`curl -F`도 지원), 지정한 폴더에 저장 |
| **Webhook 수신기** | 외부 시스템에서 보낸 Webhook 요청(헤더 + 본문)을 수신하고 기록 |
| **리버스 프록시** | 요청을 대상 URL로 전달하고 주고받은 내용을 기록 |
| **지연 시뮬레이션** | 전달 전에 인위적인 지연을 추가해 느린 네트워크 환경을 테스트 |

### 앱 특징

- **앱 센터**: 모든 도구를 카드로 표시, 검색·상단 고정·드래그 정렬 지원
- **사용자 도구**: 로컬 프로그램(.exe)이나 URL을 추가해 내장 도구와 함께 관리
- **다국어**: 简体中文 / English / 日本語 / 한국어, 재시작 없이 즉시 전환
- **다크 / 라이트 테마**: 제목 표시줄에서 클릭 한 번으로 전환
- **항상 위**, **Windows 시작 시 자동 실행**, **시스템 트레이로 최소화**
- **설정 저장**: 모든 설정을 LiteDB 데이터베이스 `%LOCALAPPDATA%\HTools\settings.db`에 저장

## 🖥 실행 환경

- Windows 10 / 11 (x64)
- 소스에서 빌드하려면 [.NET 10 SDK](https://dotnet.microsoft.com/download)가 필요합니다

> 일부 하드웨어 온도 센서는 관리자 권한으로 실행하거나 메인보드 / 그래픽 제조사 드라이버가 있어야 읽을 수 있습니다.

## 🚀 빌드 및 실행

```bash
git clone https://github.com/blandh26/H.Tools.git
cd H.Tools
dotnet run --project src/HTools.App
```

단위 테스트 실행:

```bash
dotnet test
```

단독 실행 파일로 게시:

```bash
dotnet publish src/HTools.App -c Release -r win-x64 --self-contained
```

## 📁 프로젝트 구조

```
src/
├─ HTools.App       Avalonia UI (뷰, 뷰 모델, 앱 서비스)
├─ HTools.Core      모델, 다국어 서비스와 언어 파일, LiteDB 설정 저장소
├─ HTools.Windows   Win32 연동: 전역 단축키, 스크린샷 오버레이, 클립보드, SVG 렌더링
└─ HTools.Server    Kestrel 기반 로컬 서버 모듈
tests/
└─ HTools.Core.Tests  xUnit 단위 테스트
```

## 🧩 기술 스택

- [Avalonia UI 12](https://avaloniaui.net/) + Fluent 테마
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- ASP.NET Core Kestrel (로컬 서버 모듈)
- [LiteDB](https://www.litedb.org/) (설정 저장)
- [Svg.NET](https://github.com/svg-net/SVG) (아이콘 렌더링)
- [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), System.Management (하드웨어 정보)

서드파티 라이선스 정보는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)를 참고하세요.

## 🌐 언어 추가

언어 파일은 `src/HTools.Core/Resources/Lang/`에 있습니다(JSON 형식, 포함 리소스). 기존 파일을 복사해 모든 값을 번역한 뒤 `LocalizationService`의 `SupportedLanguages`에 등록하면 됩니다.

## 📮 연락처

- 이메일: [blandh26@gmail.com](mailto:blandh26@gmail.com)
- 웹사이트: [www.kimchicoder.com](https://www.kimchicoder.com)
