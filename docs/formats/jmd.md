# JMD 파일 포맷

## 개요
JMD는 RayCity 게임에서 사용하는 압축/암호화된 데이터 컨테이너 포맷

## 파일 구조

### 식별자 영역 (0x00-0x7F)
```c
struct JMDIdentifier {
    wchar_t identifier[32];     // 레이어 식별자
    wchar_t secondText[32];     // 보조 텍스트
};
```

### 헤더 (0x80-0xFF)
```c
struct JMDHeader {
    uint8_t  checkCode;      // 0x53
    uint8_t  processMode;    // 처리 플래그
    uint32_t hash;          // Adler32 해시
    uint32_t encryptKey;    // [옵션] 암호화 키
    uint32_t decompressSize;// [옵션] 압축 해제 크기
};
```

### 데이터 정보 영역 (0x100-)
```c
struct JMDDataInfo {
    uint32_t index;         // 데이터 인덱스
    uint32_t offset;        // 데이터 오프셋 (<<8)
    uint32_t dataSize;      // 데이터 크기
    uint32_t uncompSize;    // 압축 해제 크기
    uint32_t property;      // 블록 속성
    uint32_t checksum;      // Adler32 체크섬
};
```

### 처리 모드 플래그
| 비트 | 값 | 설명 |
|------|-----|------|
| 0 | 0x01 | zlib 압축 사용 |
| 1 | 0x02 | JMD 암호화 사용 |
| 0+1 | 0x03 | 압축+암호화 조합 |

### 블록 속성
```c
enum JmdDataInfoProperty {
    None = 0,               // 처리 없음
    Compressed = 1,         // zlib 압축
    FullEncrypted = 2,     // 전체 암호화
    PartialEncrypted = 3,  // 부분 암호화
    CompressedEncrypted = 4 // 압축+암호화
}
```

### 파일 속성
```c
enum JmdFileProperty {
    None = 0x00,           // 처리 없음
    Compressed = 0x01,     // 압축
    Encrypted = 0x04,      // 암호화
    PartialEncrypted = 0x05, // 부분 암호화
    CompressedEncrypted = 0x06 // 압축+암호화
}
```

## 키 생성

### JMD 키
```c
uint GetJmdKey(string fileName) {
    byte[] utf16 = Encode(fileName, "UTF-16");
    return Adler32(utf16) + 0x3de90dc3;
}
```

### 디렉토리 키
```c
uint GetDirectoryKey(uint jmdKey) {
    return jmdKey - 0x41014EBF;
}
```

### 파일 키
```c
uint GetFileKey(uint jmdKey, string fileName, uint extNum) {
    byte[] utf16 = Encode(fileName, "UTF-16");
    uint key = Adler32(utf16);
    key += extNum;
    key += (jmdKey - 0x7E2AF33D);
    return key;
}
```

## 데이터 처리

### 압축시
1. 원본 데이터 준비
2. zlib 압축 적용
3. 압축 해제 크기 저장
4. 압축 데이터 저장

### 암호화시
1. 4바이트 키 생성/입력
2. XOR 암호화 적용
3. 암호화 키 저장
4. 암호화 데이터 저장

### 압축+암호화시
1. 원본 데이터 준비
2. zlib 압축 적용
3. XOR 암호화 적용
4. 메타데이터 저장
5. 처리된 데이터 저장

### 부분 암호화시
1. 처음 0x100바이트만 암호화
2. 나머지는 평문 저장
3. 두 블록으로 분할 저장

## 파일 시스템

### 디렉토리 구조
```c
struct DirectoryData {
    int32_t folderCount;   // 폴더 수
    struct {
        wstring name;      // 폴더명
        uint32_t dataIdx;  // 데이터 인덱스
    } folders[];
    
    int32_t fileCount;     // 파일 수
    struct {
        wstring name;      // 파일명
        uint32_t extNum;   // 확장자
        int32_t property;  // 파일 속성
        uint32_t dataIdx;  // 데이터 인덱스
        int32_t size;      // 파일 크기
    } files[];
};
```

### 데이터 소스
- 스트림 생성
- 데이터 읽기/쓰기
- 바이트 배열 변환
- 메모리 관리

## 제한사항
- 최대 파일 크기: 제한 없음
- 압축 알고리즘: zlib만 지원
- 암호화: 단순 XOR 방식
- 헤더 크기: 가변 (6-14바이트)
- 블록 정렬: 256바이트 