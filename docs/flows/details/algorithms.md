# 처리 알고리즘

## 파일 열기

### 1. 초기화
```
1. 파일 핸들러 생성
2. 헤더 읽기/검증
3. 디렉토리 구조 구성
4. 데이터 소스 초기화
```

### 2. 헤더 처리
```c
void ProcessHeader() {
    // 1. 식별자 검증
    if (!ValidateIdentifier())
        throw new Exception("Invalid identifier");
        
    // 2. 체크코드 확인
    if (header.checkCode != 0x53)
        throw new Exception("Invalid check code");
        
    // 3. 해시 검증
    if (!ValidateHash())
        throw new Exception("Invalid hash");
}
```

## 데이터 처리

### 1. 블록 읽기
```c
DataBlock ReadBlock(uint index) {
    // 1. 데이터 정보 확인
    var info = GetDataInfo(index);
    if (info == null)
        throw new Exception("Invalid data index");
        
    // 2. 블록 읽기
    var block = new DataBlock {
        offset = info.offset << 8,
        size = info.dataSize,
        flags = info.property
    };
    
    return block;
}
```

### 2. 압축 해제
```c
byte[] Decompress(byte[] data, int uncompSize) {
    // 1. zlib 스트림 초기화
    using var ms = new MemoryStream(data);
    using var zs = new ZlibStream(ms, CompressionMode.Decompress);
    
    // 2. 압축 해제
    var output = new byte[uncompSize];
    zs.Read(output, 0, uncompSize);
    
    // 3. 크기 검증
    if (output.Length != uncompSize)
        throw new Exception("Invalid decompressed size");
        
    return output;
}
```

### 3. 복호화
```c
byte[] Decrypt(byte[] data, uint key) {
    // 1. 키 준비
    var extKey = ExtendKey(key);
    
    // 2. XOR 복호화
    for (int i = 0; i < data.Length; i++)
        data[i] ^= extKey[i % extKey.Length];
        
    return data;
}
```

## 디렉토리 처리

### 1. 구조 구성
```c
void BuildStructure(byte[] data) {
    // 1. 폴더 처리
    int folderCount = ReadInt32(data);
    for (int i = 0; i < folderCount; i++) {
        var folder = new JmdFolder {
            name = ReadString(data),
            dataIndex = ReadUInt32(data)
        };
        AddFolder(folder);
    }
    
    // 2. 파일 처리
    int fileCount = ReadInt32(data);
    for (int i = 0; i < fileCount; i++) {
        var file = new JmdFile {
            name = ReadString(data),
            extNum = ReadUInt32(data),
            property = ReadInt32(data),
            dataIndex = ReadUInt32(data),
            size = ReadInt32(data)
        };
        AddFile(file);
    }
}
```

### 2. 파일 처리
```c
JmdFileHandler CreateHandler(JmdFile file) {
    // 1. 핸들러 생성
    var handler = new JmdFileHandler {
        fileDataIndex = file.dataIndex,
        key = GetFileKey(jmdKey, file.name, file.extNum),
        size = file.size,
        property = file.property
    };
    
    // 2. 데이터 소스 연결
    handler.SetDataSource(dataSource);
    
    return handler;
}
```

## 메모리 관리

### 1. 리소스 할당
```c
void AllocateResources() {
    // 1. 스트림 생성
    fileStream = File.OpenRead(path);
    
    // 2. 버퍼 할당
    buffer = new byte[BUFFER_SIZE];
    
    // 3. 캐시 초기화
    cache = new Dictionary<uint, byte[]>();
}
```

### 2. 리소스 해제
```c
void Dispose() {
    // 1. 스트림 정리
    if (fileStream != null) {
        fileStream.Dispose();
        fileStream = null;
    }
    
    // 2. 캐시 정리
    if (cache != null) {
        cache.Clear();
        cache = null;
    }
    
    // 3. 버퍼 정리
    buffer = null;
}
``` 