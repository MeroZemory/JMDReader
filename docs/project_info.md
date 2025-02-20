# JMDReader 프로젝트 정보

## 개요
- JMD 파일 뷰어/추출기
- .NET 8.0 기반 Windows Forms 애플리케이션
- GNU GPL v3 라이선스

## 주요 기능
- JMD 파일 열기/탐색
- 파일/폴더 추출
- 이미지 변환 (DDS/TGA -> PNG)
- BML -> XML 변환
- 다국어 지원

## 프로젝트 구조

### 디렉토리
- src/: 소스 코드
  - JmdLoader/: 메인 프로그램
  - RaycityLibrary/: 공통 라이브러리
- docs/: 문서
- temp/: 임시 파일
- out_pool/: 출력 파일

### 주요 파일
- JMDReader.sln: 솔루션 파일
- VersionInfo.json: 버전 정보
- LICENSE: GNU GPL v3 라이선스

## 데이터 포맷

### JMD 파일 구조
- 압축/암호화된 데이터 컨테이너
- 헤더 구조:
  - checkCode (1바이트)
  - processMode (1바이트): 암호화(2)/압축(1) 플래그
  - hash (4바이트): Adler32 해시
  - encryptKey (옵션): 암호화 키
  - decompressSize (옵션): 압축 해제 크기
  - data: 실제 데이터

### 지원 파일 형식
- DDS/TGA 이미지
- BML (바이너리 XML)
- 일반 파일

## 업데이트
- GitHub 저장소에서 버전 정보 확인
- 자동 업데이트 지원 