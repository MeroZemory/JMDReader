# BML (Binary XML) 포맷

## 개요
XML 데이터를 바이너리 형태로 최적화한 포맷

## 파일 구조

### 헤더
```c
struct BMLHeader {
    uint16_t encoding;     // UTF-16 인코딩
    uint16_t version;      // BML 버전
};
```

### 태그 구조
```c
struct BMLTag {
    uint16_t nameLength;   // 태그명 길이
    wchar_t  name[];      // 태그명 (UTF-16)
    uint16_t attrCount;   // 속성 개수
    BMLAttr  attrs[];     // 속성 배열
    uint16_t childCount;  // 자식 태그 수
    BMLTag   children[];  // 자식 태그 배열
};
```

### 속성 구조
```c
struct BMLAttr {
    uint16_t nameLength;   // 속성명 길이
    wchar_t  name[];      // 속성명 (UTF-16)
    uint16_t valueLength; // 값 길이
    wchar_t  value[];    // 값 (UTF-16)
};
```

## 데이터 형식

### 문자열
- UTF-16 인코딩
- 길이 접두사 포함
- null 종료 없음

### 숫자
- 16비트 정수
- 리틀 엔디안
- 부호 없음

### 배열
- 개수 접두사
- 연속 저장
- 가변 길이

## 특징

### 최적화
- 문자열 중복 제거
- 바이너리 저장
- 빠른 파싱

### 구조
- 계층적 구성
- 속성 지원
- 확장 가능

### 장점
- 크기 효율
- 빠른 처리
- 메모리 효율

## XML 변환

### BML -> XML
```xml
<tag attr="value">
  <child>text</child>
</tag>
```

### 처리 과정
1. 헤더 읽기
2. 태그 트리 구성
3. 속성 처리
4. 문자열 디코딩
5. XML 생성

## 제한사항
- 인코딩: UTF-16 고정
- 주석 미지원
- CDATA 미지원
- DTD 미지원 