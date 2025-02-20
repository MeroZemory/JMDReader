# JMDReader 개발 정보

## 개발 환경
- .NET 8.0
- Windows Forms
- Visual Studio 2022
- C#

## 프로젝트 구성
- JmdLoader (실행 프로그램)
  - UI 구현
  - 파일 처리
  - 다국어 지원
  - 업데이트 관리
- RaycityLibrary (공통 라이브러리)
  - 파일 포맷 처리
  - 암호화/압축
  - 데이터 구조 정의

## 주요 클래스

### JmdLoader
- Program: 프로그램 진입점
- MainWindow: 메인 UI
- JmdArchive: JMD 파일 처리
- LanguageManager: 다국어 관리
- UpdateManager: 업데이트 처리

### RaycityLibrary
- RaycityObject: 기본 객체 클래스
- DataProcessor: 데이터 처리
- JmdEncrypt: 암호화 처리
- Adler: 체크섬 계산

## 빌드 설정
- 플랫폼: x86/x64
- 단일 파일 배포
- 자체 포함 불필요
- 런타임: .NET 8.0

## 배포
- GitHub 저장소 사용
- 자동 업데이트 지원
- 버전 정보 JSON 관리 