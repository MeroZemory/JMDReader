# 압축 알고리즘

## zlib 압축

### 개요
- DEFLATE 알고리즘 기반
- 무손실 데이터 압축
- 스트림 처리 지원

### 스트림 구조
```c
struct ZlibStream {
    Header {
        CMF: uint8     // 압축 방식
        FLG: uint8     // 플래그
    }
    Data {
        blocks[]       // DEFLATE 블록
    }
    Trailer {
        checksum: uint32 // Adler32
    }
}
```

### DEFLATE 블록
```c
struct DeflateBlock {
    Header {
        BFINAL: 1 bit  // 마지막 블록
        BTYPE: 2 bits  // 블록 타입
    }
    Data {
        // 타입별 데이터
    }
}
```

### 블록 타입
| 값 | 설명 |
|----|------|
| 00 | 비압축 |
| 01 | 고정 허프만 |
| 10 | 동적 허프만 |
| 11 | 예약됨 |

## RLE 압축 (TGA)

### 개요
- Run-Length Encoding
- 단순 반복 압축
- 픽셀 데이터 최적화

### 패킷 구조
```
RLE Packet {
    Header: uint8 {
        Count: 7 bits   // 반복 횟수 - 1
        Type: 1 bit     // 0=Raw, 1=RLE
    }
    Data: bytes[]      // 픽셀 데이터
}
```

### 처리 방식
- Raw 패킷: Count+1개 픽셀 복사
- RLE 패킷: 1개 픽셀을 Count+1번 반복

## 압축 설정

### zlib 옵션
```c
#define Z_NO_COMPRESSION      0
#define Z_BEST_SPEED         1
#define Z_BEST_COMPRESSION   9
#define Z_DEFAULT_COMPRESSION (-1)
```

### 윈도우 크기
- 기본: 32KB
- 최소: 256B
- 최대: 32KB

### 메모리 레벨
- 기본: 8
- 최소: 1
- 최대: 9

## 압축 처리

### 데이터 압축
```c
byte[] Compress(byte[] data) {
    using(MemoryStream ms = new MemoryStream()) {
        ZlibStream zs = new ZlibStream(
            ms,
            CompressionMode.Compress,
            CompressionLevel.Optimal
        );
        zs.Write(data, 0, data.Length);
        zs.Flush();
        return ms.ToArray();
    }
}
```

### 데이터 해제
```c
byte[] Decompress(byte[] data, int uncompSize) {
    byte[] output = new byte[uncompSize];
    using(MemoryStream ms = new MemoryStream(data)) {
        ZlibStream zs = new ZlibStream(
            ms,
            CompressionMode.Decompress
        );
        zs.Read(output, 0, uncompSize);
    }
    return output;
}
```

### 체크섬 계산
```c
uint CalcChecksum(byte[] data) {
    return Adler32(0, data, 0, data.Length);
}
```

## 성능 특성

### 압축률
- 텍스트: 높음
- 이미지: 중간
- 바이너리: 가변적

### 속도
- 압축: 중간
- 해제: 빠름
- 메모리: 효율적

### 장점
- 높은 호환성
- 안정적 성능
- 스트림 처리

### 단점
- CPU 사용량
- 압축률 제한
- 메모리 사용

## 제한사항
- 최대 크기: 4GB
- 윈도우: 32KB
- 메모리: 레벨별
- 스트림 버퍼 제한 