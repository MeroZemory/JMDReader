# 암호화 알고리즘

## JMD 암호화

### 개요
- XOR 기반 블록 암호화
- 4바이트 키 사용
- 양방향 암호화/복호화

### 키 구조
```c
struct JMDKey {
    uint8_t key[4];    // 4바이트 키
};
```

### 키 생성
```c
// JMD 파일 키
uint GetJmdKey(string fileName) {
    byte[] utf16 = Encode(fileName, "UTF-16");
    return Adler32(utf16) + 0x3de90dc3;
}

// 디렉토리 키
uint GetDirectoryKey(uint jmdKey) {
    return jmdKey - 0x41014EBF;
}

// 파일 키
uint GetFileKey(uint jmdKey, string fileName, uint extNum) {
    byte[] utf16 = Encode(fileName, "UTF-16");
    uint key = Adler32(utf16);
    key += extNum;
    key += (jmdKey - 0x7E2AF33D);
    return key;
}

// 키 확장 (64바이트)
byte[] ExtendKey(uint key) {
    byte[] extended = new byte[64];
    uint curKey = key ^ 0x8473fbc1;
    for(int i = 0; i < 16; i++) {
        WriteUInt32(extended, i*4, curKey);
        curKey -= 0x7b8c043f;
    }
    return extended;
}
```

### 처리 단위
- 기본 블록: 4바이트
- 부분 블록: 1-3바이트
- 정렬: 4바이트

### 암호화 과정
```c
void Encrypt(byte[] data, uint key[4]) {
    // 4바이트 블록 처리
    for(int i = 0; i < length/4*4; i += 4) {
        data[i+0] ^= key[0];
        data[i+1] ^= key[1];
        data[i+2] ^= key[2];
        data[i+3] ^= key[3];
    }
    
    // 남은 바이트 처리
    for(int i = length/4*4; i < length; i++) {
        data[i] ^= key[i % 4];
    }
}
```

### 데이터 정보 암호화
```c
void EncryptDataInfo(byte[] key, byte[] data) {
    // 32바이트 키로 XOR
    for(int i = 0; i < data.Length; i++) {
        data[i] ^= key[i];
    }
}
```

## 키 관리

### 키 생성
- 파일명 기반 생성
- Adler32 해시 사용
- 상수값 조합

### 키 범위
```c
#define MIN_KEY 0x00000000
#define MAX_KEY 0xFFFFFFFF
```

### 키 저장
- JMD 헤더 내 포함
- 암호화 플래그 필수
- 4바이트 정렬

## 보안 특성

### 강도
- 낮은 암호화 강도
- 단순 데이터 보호
- 실시간 처리 가능

### 장점
- 구현 단순
- 빠른 처리
- 적은 메모리

### 단점
- 키 노출 위험
- 패턴 분석 취약
- 중복 블록 취약

## 최적화

### 블록 처리
- SIMD 활용 가능
- 캐시 정렬 처리
- 병렬화 가능

### 메모리 접근
- 4바이트 정렬
- 연속 접근
- 캐시 친화적

## 제한사항
- 키 크기: 4바이트
- 블록 크기: 4바이트
- 암호화 강도: 낮음
- 키 저장: 평문 