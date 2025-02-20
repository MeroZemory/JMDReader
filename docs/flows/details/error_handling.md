# 에러 처리

## 파일 검증

### 1. 식별자 오류
```c
class InvalidIdentifierException : Exception {
    public InvalidIdentifierException(string message) 
        : base($"Invalid JMD identifier: {message}") {}
}

void ValidateIdentifier() {
    if (!IsValidIdentifier())
        throw new InvalidIdentifierException("Layer identifier mismatch");
        
    if (!IsValidVersion())
        throw new InvalidIdentifierException("Version not supported");
}
```

### 2. 헤더 오류
```c
class InvalidHeaderException : Exception {
    public InvalidHeaderException(string message)
        : base($"Invalid JMD header: {message}") {}
}

void ValidateHeader() {
    if (header.checkCode != 0x53)
        throw new InvalidHeaderException("Check code mismatch");
        
    if (!ValidateHash())
        throw new InvalidHeaderException("Hash verification failed");
}
```

### 3. 체크섬 오류
```c
class ChecksumException : Exception {
    public ChecksumException(uint expected, uint actual)
        : base($"Checksum mismatch: expected {expected:X8}, got {actual:X8}") {}
}

void ValidateChecksum(byte[] data, uint expected) {
    uint actual = Adler32(data);
    if (actual != expected)
        throw new ChecksumException(expected, actual);
}
```

## 데이터 처리

### 1. 압축 오류
```c
class DecompressionException : Exception {
    public DecompressionException(string message)
        : base($"Decompression failed: {message}") {}
}

void ValidateDecompression(int expected, int actual) {
    if (actual != expected)
        throw new DecompressionException(
            $"Size mismatch: expected {expected}, got {actual}");
}
```

### 2. 복호화 오류
```c
class DecryptionException : Exception {
    public DecryptionException(string message)
        : base($"Decryption failed: {message}") {}
}

void ValidateDecryption(byte[] data) {
    if (!IsValidData(data))
        throw new DecryptionException("Data validation failed");
}
```

### 3. 메모리 오류
```c
class MemoryException : Exception {
    public MemoryException(string message)
        : base($"Memory error: {message}") {}
}

void ValidateMemory(long required) {
    if (required > MAX_MEMORY)
        throw new MemoryException(
            $"Required memory ({required}) exceeds limit ({MAX_MEMORY})");
}
```

## 구조 오류

### 1. 디렉토리 오류
```c
class DirectoryException : Exception {
    public DirectoryException(string message)
        : base($"Directory error: {message}") {}
}

void ValidateDirectory() {
    if (IsCorrupted())
        throw new DirectoryException("Directory structure corrupted");
        
    if (HasInvalidEntries())
        throw new DirectoryException("Invalid directory entries");
}
```

### 2. 파일 오류
```c
class FileException : Exception {
    public FileException(string message)
        : base($"File error: {message}") {}
}

void ValidateFile(JmdFile file) {
    if (!file.IsValid())
        throw new FileException($"Invalid file: {file.name}");
        
    if (!HasValidDataIndex(file))
        throw new FileException($"Invalid data index: {file.dataIndex}");
}
```

### 3. 참조 오류
```c
class ReferenceException : Exception {
    public ReferenceException(string message)
        : base($"Reference error: {message}") {}
}

void ValidateReference(uint index) {
    if (!dataInfoMap.ContainsKey(index))
        throw new ReferenceException($"Invalid data reference: {index}");
}
```

## 오류 처리 전략

### 1. 검증 단계
```
1. 파일 열기 전 검증
2. 헤더 읽기 후 검증
3. 데이터 처리 전 검증
4. 결과 데이터 검증
```

### 2. 복구 전략
```
1. 부분 손상시 건너뛰기
2. 메모리 부족시 캐시 정리
3. 참조 오류시 대체 데이터
4. 구조 오류시 재구성
```

### 3. 리소스 정리
```
1. 예외 발생시 자동 정리
2. 임시 파일 삭제
3. 메모리 해제
4. 스트림 닫기
``` 