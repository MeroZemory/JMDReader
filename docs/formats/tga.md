# TGA (Truevision Graphics Adapter) 포맷

## 개요
비압축 래스터 이미지 저장을 위한 포맷

## 파일 구조

### 헤더
```c
struct TGAHeader {
    uint8_t  idLength;      // ID 필드 길이
    uint8_t  colorMapType;  // 컬러맵 타입
    uint8_t  imageType;     // 이미지 타입
    
    struct {
        uint16_t firstEntry;// 첫 엔트리 인덱스
        uint16_t length;    // 컬러맵 길이
        uint8_t  entrySize;// 엔트리 크기
    } colorMapSpec;
    
    struct {
        uint16_t xOrigin;   // X 시작점
        uint16_t yOrigin;   // Y 시작점
        uint16_t width;     // 너비
        uint16_t height;    // 높이
        uint8_t  depth;     // 픽셀 깊이
        uint8_t  descriptor;// 이미지 속성
    } imageSpec;
};
```

### 이미지 타입
| 값 | 설명 |
|----|------|
| 0 | 이미지 없음 |
| 1 | 비압축 컬러맵 |
| 2 | 비압축 트루컬러 |
| 3 | 비압축 흑백 |
| 9 | RLE 압축 컬러맵 |
| 10 | RLE 압축 트루컬러 |
| 11 | RLE 압축 흑백 |

### 픽셀 깊이
- 8비트: 흑백/인덱스
- 16비트: RGB555/RGBA4444
- 24비트: RGB888
- 32비트: RGBA8888

## 데이터 구조

### ID 필드
- 옵션
- 이미지 식별 정보
- 가변 길이

### 컬러맵
- 선택적 포함
- 팔레트 데이터
- 엔트리 크기별 구성

### 이미지 데이터
- 픽셀 배열
- 상향/하향 저장
- 좌우 방향 지정

### 푸터 (2.0)
```c
struct TGAFooter {
    uint32_t extOffset;    // 확장 영역 오프셋
    uint32_t devOffset;    // 개발자 영역 오프셋
    char     signature[18];// "TRUEVISION-XFILE.\0"
};
```

## 특징
- 단순한 구조
- 다양한 색상 깊이
- 알파 채널 지원
- RLE 압축 옵션

## 제한사항
- 최대 크기: 제한 없음
- 압축: RLE만 지원
- 컬러맵: 256색 제한
- 확장: 2.0부터 지원 