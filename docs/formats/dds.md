# DDS (DirectDraw Surface) 포맷

## 개요
DirectX 텍스처 저장을 위한 이미지 포맷

## 파일 구조

### 헤더
```c
struct DDSHeader {
    uint32_t magic;           // "DDS " (0x20534444)
    uint32_t size;           // 124
    uint32_t flags;          // 필수 플래그
    uint32_t height;         // 높이 (픽셀)
    uint32_t width;          // 너비 (픽셀)
    uint32_t pitchOrLinearSize; // 피치 또는 선형 크기
    uint32_t depth;          // 깊이 (3D 텍스처)
    uint32_t mipMapCount;    // 밉맵 레벨 수
    uint32_t reserved1[11];  // 예약됨
    
    struct {
        uint32_t size;       // 32
        uint32_t flags;      // 픽셀 포맷 플래그
        uint32_t fourCC;     // FourCC 코드
        uint32_t rgbBitCount;// RGB 비트 수
        uint32_t rBitMask;   // R 채널 마스크
        uint32_t gBitMask;   // G 채널 마스크
        uint32_t bBitMask;   // B 채널 마스크
        uint32_t aBitMask;   // A 채널 마스크
    } pixelFormat;
    
    uint32_t caps;          // 기능 플래그
    uint32_t caps2;         // 추가 기능
    uint32_t caps3;         // 미사용
    uint32_t caps4;         // 미사용
    uint32_t reserved2;     // 예약됨
};
```

### 주요 플래그
```c
#define DDSD_CAPS        0x1
#define DDSD_HEIGHT      0x2
#define DDSD_WIDTH       0x4
#define DDSD_PITCH       0x8
#define DDSD_PIXELFORMAT 0x1000
#define DDSD_MIPMAPCOUNT 0x20000
#define DDSD_LINEARSIZE  0x80000
```

## 압축 포맷

### DXT1 (BC1)
- 4bpp (bits per pixel)
- RGB 압축
- 1비트 알파 (선택)
- 4x4 픽셀 블록

### DXT3 (BC2)
- 8bpp
- RGB 압축
- 4비트 알파
- 4x4 픽셀 블록

### DXT5 (BC3)
- 8bpp
- RGB 압축
- 보간된 알파
- 4x4 픽셀 블록

## 데이터 레이아웃

### 비압축 포맷
- 연속된 픽셀 데이터
- 행 피치 정렬
- 선택적 밉맵

### 압축 포맷
- 4x4 블록 배열
- 선형 메모리 레이아웃
- 연속된 밉맵 체인

## 특징
- 하드웨어 가속 지원
- 밉맵 내장 가능
- 다양한 픽셀 포맷
- 효율적인 메모리 사용

## 제한사항
- 최대 크기: GPU 의존
- 압축: 블록 기반
- 알파: 포맷별 상이 