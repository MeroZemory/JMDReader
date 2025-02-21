using Raycity.Encrypt;
using Raycity.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Raycity.File
{
    /// <summary>
    /// <see cref="JmdFile"/> represents a Jmd type archive. You can open and save Jmd file with this class.
    /// </summary>
    public partial class JmdArchive : IJmdArchive<JmdFile, JmdFolder>
    {
        #region Members
        private int _layerVersion; // 1.0 = 0, 1.1 = 1
        private FileStream? _jmdStream;

        private Dictionary<uint, JmdDataInfo> _dataInfoMap;
        private JmdFolder _rootFolder;

        private Dictionary<uint, JmdFileHandler> _fileHandlers;

        private uint _jmdKey;
        private uint _dataChecksum;

        private bool _disposed;
        #endregion

        #region Properties
        /// <summary>
        /// Root folder of current <see cref="JmdArchive"/>
        /// </summary>
        public JmdFolder RootFolder => _rootFolder;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new instance of <see cref="JmdArchive"/>. 
        /// </summary>
        public JmdArchive()
        {
            _rootFolder = new JmdFolder();
            _dataInfoMap = [];
            _fileHandlers = [];
        }
        #endregion

        #region Methods
        /// <summary>
        /// Opens Jmd file.
        /// </summary>
        /// <param name="filePath">The file path of Jmd file.</param>
        /// <exception cref="FileNotFoundException"> It will be thrown if required file can't be found. </exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public void Open(string filePath)
        {
            Debug.WriteLine($"=== JMD Reader 로그 ===");
            Debug.WriteLine($"파일 경로: {filePath}");

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"");
            _jmdStream = new FileStream(filePath, FileMode.Open);
            if (_jmdStream.Length < 0x80)
                throw new InvalidOperationException();

            _jmdKey = JmdKey.GetJmdKey(Path.GetFileNameWithoutExtension(filePath));
            Debug.WriteLine($"JMD 키: 0x{_jmdKey:X8}");

            // Checks identifier
            BinaryReader reader = new(_jmdStream);

            _jmdStream.Seek(0x0, SeekOrigin.Begin);
            byte[] identifierData = reader.ReadBytes(0x40);
            string converftedStr = Encoding.Unicode.GetString(identifierData, 0, RhLayerIdentifiers[0].Length << 1);
            Debug.WriteLine($"식별자: {converftedStr}");

            int layerVersion = -1;
            for (int i = 0; i < RhLayerIdentifiers.Length; i++)
                if (converftedStr == RhLayerIdentifiers[i])
                {
                    layerVersion = i;
                    break;
                }
            if (layerVersion != 0)
                throw new Exception();
            else
            {
                _layerVersion = layerVersion;
                Debug.WriteLine($"=== 파일 식별 정보 ===");
                Debug.WriteLine($"레이어 버전: {_layerVersion}");
                Debug.WriteLine($"식별자 위치: 0x00-0x3F");
                Debug.WriteLine($"보조 텍스트 위치: 0x40-0x7F");
                Debug.WriteLine($"보조 텍스트: {RhLayerSecondText}");
            }

            // Read jmd archive info
            _jmdStream.Seek(0x80, SeekOrigin.Begin);
            byte[] jmdArchiveInfoData = reader.ReadBytes(0x80);
            Debug.WriteLine($"\n=== 아카이브 헤더 정보 (0x80-0xFF) ===");
            Debug.WriteLine($"암호화된 아카이브 정보 처음 4바이트: {BitConverter.ToString(jmdArchiveInfoData, 0, 4)}");

            jmdArchiveInfoData = JmdEncrypt.DecryptData(_jmdKey, jmdArchiveInfoData);
            Debug.WriteLine($"복호화된 아카이브 정보 처음 4바이트: {BitConverter.ToString(jmdArchiveInfoData, 0, 4)}");

            int dataInfoCount = 0;
            byte[] dataInfoKey = [];

            // Decode jmd archive info
            using (MemoryStream memStream = new(jmdArchiveInfoData))
            {
                BinaryReader memReader = new(memStream);   
                uint infoDataChksum = memReader.ReadUInt32();
                uint verifyChkSum = Adler.Adler32(0, jmdArchiveInfoData, 4, 0x7C); 
                Debug.WriteLine($"체크섬: 0x{infoDataChksum:X8}, 계산된 체크섬: 0x{verifyChkSum:X8}");
                if (infoDataChksum != verifyChkSum)
                    throw new Exception("jmd file modified.");
                int versionChkCode = memReader.ReadInt32();
                dataInfoCount = memReader.ReadInt32();
                uint dataInfoWhiteningKey = memReader.ReadUInt32();
                Debug.WriteLine($"버전 체크코드: 0x{versionChkCode:X8}");
                Debug.WriteLine($"데이터 정보 개수: {dataInfoCount}");
                Debug.WriteLine($"데이터 정보 키: 0x{dataInfoWhiteningKey:X8}");
                Debug.WriteLine($"매직코드: 0xd24e8143");
                
                dataInfoKey = memReader.ReadBytes(0x20);
                uint endMagicCode = memReader.ReadUInt32();
                int u4 = memReader.ReadInt32();
                if (endMagicCode != 0xd24e8143u)
                    throw new Exception("invalid archiveInfo end magic code.");
            }

            // Read data information collection.
            Debug.WriteLine($"\n=== 데이터 블록 정보 (0x100-) ===");
            _dataInfoMap = new Dictionary<uint, JmdDataInfo>(dataInfoCount);
            _fileHandlers = new Dictionary<uint, JmdFileHandler>(dataInfoCount);
            for(int i = 0; i < dataInfoCount; i++)
            {
                JmdDataInfo dataInfo = reader.ReadBlockInfo(dataInfoKey);
                Debug.WriteLine($"\n데이터 블록 {i}:");
                Debug.WriteLine($"  인덱스: 0x{dataInfo.Index:X8}");
                Debug.WriteLine($"  오프셋: 0x{dataInfo.Offset:X8}");
                Debug.WriteLine($"  데이터 크기: {dataInfo.DataSize} 바이트");
                Debug.WriteLine($"  압축해제 크기: {dataInfo.UncompressedSize} 바이트");
                Debug.WriteLine($"  블록 속성: {dataInfo.BlockProperty}");
                Debug.WriteLine($"  체크섬: 0x{dataInfo.Checksum:X8}");
                _dataInfoMap.Add(dataInfo.Index, dataInfo);
            }

            // Read all folders and all files info.
            uint folderKey = JmdKey.GetDirectoryDataKey(_jmdKey);
            Debug.WriteLine($"\n=== 폴더 구조 파싱 시작 ===");
            Debug.WriteLine($"폴더 키: 0x{folderKey:X8}");

            Queue<(uint folderDataIndex, JmdFolder folder)> procssQueue = new();
            procssQueue.Enqueue((0xFFFFFFFF, _rootFolder));

            while(procssQueue.Count > 0)
            {
                var queObj = procssQueue.Dequeue();
                Debug.WriteLine($"\n=== 폴더 파싱 시작: {queObj.folder.Name} ===");
                Debug.WriteLine($"폴더 데이터 읽기: {queObj.folder.Name}, 인덱스=0x{queObj.folderDataIndex:X8}");
                byte[] folderData = getData(queObj.folderDataIndex, folderKey);
                Debug.WriteLine($"읽은 데이터 크기: {folderData.Length} 바이트");
                Debug.WriteLine($"복호화된 데이터 처음 16바이트: {BitConverter.ToString(folderData, 0, Math.Min(4, folderData.Length))}");

                using(MemoryStream memStream = new(folderData))
                {
                    BinaryReader memReader = new(memStream);
                    int folderCount = memReader.ReadInt32();
                    Debug.WriteLine($"하위 폴더 수: {folderCount} (0x{folderCount:X8})");
                    Debug.WriteLine($"현재 스트림 위치: 0x{memStream.Position:X8}");

                    for(int i = 0; i < folderCount && folderCount > 0 && folderCount < 1000000; i++)
                    {
                        JmdFolder subFolder = new();
                        string name = memReader.ReadNullTerminatedText(true);
                        uint folderDataIndex = memReader.ReadUInt32();
                        Debug.WriteLine($"  하위 폴더 {i+1}: 이름={name}, 인덱스=0x{folderDataIndex:X8}");
                        subFolder.Name = name;
                        procssQueue.Enqueue((folderDataIndex, subFolder));
                        queObj.folder.AddFolder(subFolder);
                    }

                    int fileCount = memReader.ReadInt32();
                    Debug.WriteLine($"파일 수: {fileCount} (0x{fileCount:X8})");
                    Debug.WriteLine($"현재 스트림 위치: 0x{memStream.Position:X8}");

                    for(int i = 0; i < fileCount && fileCount > 0 && fileCount < 1000000; i++)
                    {
                        JmdFile subFile = new();
                        string fileName = memReader.ReadNullTerminatedText(true);
                        uint extInt = memReader.ReadUInt32();
                        int fileProperty = memReader.ReadInt32();
                        uint dataIndex = memReader.ReadUInt32();
                        int fileSize = memReader.ReadInt32();
                        uint fileKey = JmdKey.GetFileKey(_jmdKey, fileName, extInt);
                        string fileExtension = Encoding.ASCII.GetString(BitConverter.GetBytes(extInt)).TrimEnd('\0');
                        Debug.WriteLine($"  파일 {i+1}: {fileName}.{fileExtension}, 크기={fileSize}, 속성={fileProperty}, 인덱스=0x{dataIndex:X8}, 키=0x{fileKey:X8}");

                        JmdFileHandler fileHandler = new(this, (JmdFileProperty)fileProperty, dataIndex, fileSize, fileKey);
                        JmdDataSource bufferedDataSource = new(fileHandler);
                        subFile.DataSource = bufferedDataSource;
                        subFile.Name = $"{fileName}.{fileExtension}";
                        subFile.FileEncryptionProperty = (JmdFileProperty)fileProperty;

                        _fileHandlers.Add(dataIndex, fileHandler);
                        queObj.folder.AddFile(subFile);
                    }

                    Debug.WriteLine($"=== 폴더 파싱 완료: {queObj.folder.Name} ===");
                    Debug.WriteLine($"최종 하위 폴더 수: {queObj.folder.Folders.Count}, 파일 수: {queObj.folder.Files.Count}");
                }
            }
        }
        /// <summary>
        /// Save current <see cref="JmdArchive"/> instance to Jmd file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="Exception"></exception>
        public void SaveTo(string filePath)
        {
            string fullName = Path.GetFullPath(filePath);
            string fullDirName = Path.GetDirectoryName(fullName) ?? "";
            if(!Directory.Exists(fullDirName))
            {
                throw new Exception("directory not exists.");
            }
            if(_jmdStream is not null)
            {
                string curJmdFullName = Path.GetFullPath(_jmdStream.Name);
                if (curJmdFullName == fullDirName)
                    System.IO.File.Copy(curJmdFullName, $"{curJmdFullName}.bak");
            }
            string outFileName = Path.GetFileNameWithoutExtension(fullName);
            uint outJmdKey = JmdKey.GetJmdKey(outFileName);

            Queue<DataSavingInfo> dataSavingQueue = [];
            HashSet<uint> usedIndex = [];
            int dataEndOffset = 0;
            storeFolderAndFiles(RootFolder, dataSavingQueue, usedIndex, ref dataEndOffset, outJmdKey);
            if (_jmdStream is not null)
            {
                _jmdStream.Close();
                releaseAllHandlers();
            }
            uint outDataHash = 0;
            foreach (DataSavingInfo dataSavingInfo in dataSavingQueue)
                outDataHash = Adler.Adler32Combine(outDataHash, dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
            if(_dataInfoMap is not null)
                _dataInfoMap.Clear();
            else
                _dataInfoMap = new Dictionary<uint, JmdDataInfo>(dataSavingQueue.Count);
            if(_fileHandlers is null)
                _fileHandlers = new Dictionary<uint, JmdFileHandler>(dataSavingQueue.Count);
            // Begin write to out file.
            FileStream outFileStream = new(fullName, FileMode.Create);
            int dataInfoSize = (dataSavingQueue.Count * 0x20 + 0xFF) & 0x7FFFFF00;
            int dataBeginOffset = 0x100 + dataInfoSize;
            dataEndOffset += dataBeginOffset;

            // Write Identifier Text
            BinaryWriter outWriter = new(outFileStream);
            outWriter.Write(Encoding.Unicode.GetBytes(RhLayerIdentifiers[_layerVersion]));
            outFileStream.Seek(0x40, SeekOrigin.Begin);
            outWriter.Write(Encoding.Unicode.GetBytes(RhLayerSecondText));
            
            // Write Header
            string fileNameWithExt = Path.GetFileNameWithoutExtension(fullName) + ".jmd";
            uint dataInfoWhiteningKey = 0x6c0b8043 + Adler.Adler32(0, Encoding.Unicode.GetBytes(fileNameWithExt), 0, fileNameWithExt.Length << 1);
            outFileStream.Seek(0x80, SeekOrigin.Begin);
            byte[] jmdHeaderData = new byte[0x80]; //Without header checksum
            byte[] dataInfoKey = new byte[0x20];
            generateDataInfoKey(dataInfoKey);
            using (MemoryStream memStream = new(0x7C))
            {
                BinaryWriter memWriter = new(memStream);
                memWriter.Write(_layerVersion | 0x100);
                memWriter.Write(dataSavingQueue.Count);
                memWriter.Write(dataInfoWhiteningKey);
                memWriter.Write(dataInfoKey);
                memWriter.Write(0xd24e8143u);
                memWriter.Write(0x00000001);
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.Read(jmdHeaderData, 4, (int)memStream.Length);
            }
            uint jmdHeaderChksum = Adler.Adler32(0, jmdHeaderData, 4, 0x7C);
            Array.Copy(BitConverter.GetBytes(jmdHeaderChksum), 0, jmdHeaderData, 0, 0x04);
            JmdEncrypt.EncryptData(outJmdKey, jmdHeaderData, 0, jmdHeaderData.Length);
            outWriter.Write(jmdHeaderData);

            // Write Data Info
            outFileStream.Seek(0x100, SeekOrigin.Begin);
            foreach (DataSavingInfo dataSavingInfo in dataSavingQueue)
            {
                byte[] dataInfoEncData = new byte[0x20];
                using(MemoryStream memStream = new(0x20))
                {
                    BinaryWriter memWriter = new(memStream);
                    memWriter.Write(dataSavingInfo.DataInfo.Index);
                    memWriter.Write((int)((dataSavingInfo.DataInfo.Offset + dataBeginOffset) >> 8));
                    memWriter.Write(dataSavingInfo.DataInfo.DataSize);
                    memWriter.Write(dataSavingInfo.DataInfo.UncompressedSize);
                    memWriter.Write((int)dataSavingInfo.DataInfo.BlockProperty);
                    memWriter.Write(dataSavingInfo.DataInfo.Checksum);
                    
                    memStream.Seek(0, SeekOrigin.Begin);
                    memStream.Read(dataInfoEncData, 0, dataInfoEncData.Length);
                }
                JmdDataInfo jmdDataInfo = new()
                {
                    Index = dataSavingInfo.DataInfo.Index,
                    Offset = dataSavingInfo.DataInfo.Offset + dataBeginOffset,
                    DataSize = dataSavingInfo.DataInfo.DataSize,
                    UncompressedSize = dataSavingInfo.DataInfo.UncompressedSize,
                    BlockProperty = dataSavingInfo.DataInfo.BlockProperty,
                    Checksum = dataSavingInfo.DataInfo.Checksum
                };
                _dataInfoMap.Add(jmdDataInfo.Index, jmdDataInfo);

                JmdEncrypt.EncryptDataInfo(dataInfoKey, dataInfoEncData, 0, dataInfoEncData.Length);
                outWriter.Write(dataInfoEncData);
            }

            // Write Data
            while(dataSavingQueue.Count > 0)
            {
                DataSavingInfo dataSavingInfo = dataSavingQueue.Dequeue();
                outFileStream.Seek(dataSavingInfo.DataInfo.Offset + dataBeginOffset, SeekOrigin.Begin);
                outFileStream.Write(dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
                if(dataSavingInfo.File is not null)
                {
                    JmdFile file = dataSavingInfo.File;
                    JmdFileHandler fileHandler = new(this, file.FileEncryptionProperty, dataSavingInfo.DataInfo.Index, file.Size, JmdKey.GetFileKey(outJmdKey, file.NameWithoutExt, file.getExtNum()));
                    _fileHandlers.Add(dataSavingInfo.DataInfo.Index, fileHandler);
                    file.DataSource = new JmdDataSource(fileHandler);
                }
            }
            if(outFileStream.Position != dataEndOffset)
            {
                outFileStream.Seek(dataEndOffset - 1, SeekOrigin.Begin);
                outFileStream.WriteByte(0x00);
            }
            outFileStream.Close();
            _jmdStream = new FileStream(fullName, FileMode.Open);
        }

        public void Dispose()
        {
            if(_jmdStream is not null && _jmdStream.CanRead)
                _jmdStream.Close();
            releaseAllHandlers();
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        internal Stream? getJmdStream()
        {
            return _jmdStream;
        }

        internal byte[] getData(JmdFileHandler handler)
        {
            if (!_dataInfoMap.ContainsKey(handler._fileDataIndex))
                throw new Exception("handler corrupted.");
            return getData(handler._fileDataIndex, handler._key);
        }

        private byte[] getData(uint dataIndex, uint key)
        {
            if (!_dataInfoMap.ContainsKey(dataIndex))
                throw new Exception("index not exist.");
            if (_jmdStream is null)
                throw new Exception("jmd file not opened.");
            FileStream clonedJmdStream = new(_jmdStream.SafeFileHandle, FileAccess.Read);
            JmdDataInfo dataInfo = _dataInfoMap[dataIndex];

            Debug.WriteLine($"\n=== 데이터 블록 처리 ===");
            Debug.WriteLine($"블록 정보:");
            Debug.WriteLine($"  인덱스: 0x{dataIndex:X8}");
            Debug.WriteLine($"  오프셋: 0x{dataInfo.Offset:X8}");
            Debug.WriteLine($"  크기: {dataInfo.DataSize} 바이트");
            Debug.WriteLine($"  블록 속성: {dataInfo.BlockProperty}");
            Debug.WriteLine($"  처리 키: 0x{key:X8}");

            clonedJmdStream.Seek(dataInfo.Offset, SeekOrigin.Begin);
            byte[] outData = new byte[dataInfo.DataSize];
            clonedJmdStream.Read(outData, 0, dataInfo.DataSize);
            Debug.WriteLine($"원본 데이터 크기: {outData.Length} 바이트");
            
            if ((dataInfo.BlockProperty & JmdDataInfoProperty.Compressed) != JmdDataInfoProperty.None)
            {
                Debug.WriteLine("\n압축 해제 처리:");
                Debug.WriteLine($"  압축 데이터 크기: {outData.Length} 바이트");
                using(MemoryStream memStream = new(outData))
                {
                    outData = new byte[dataInfo.UncompressedSize];
                    ZLibStream decompressStream = new(memStream, CompressionMode.Decompress);
                    decompressStream.Read(outData, 0, outData.Length);
                }
                Debug.WriteLine($"  압축 해제 후 크기: {outData.Length} 바이트");
            }
            if((dataInfo.BlockProperty & JmdDataInfoProperty.PartialEncrypted) != JmdDataInfoProperty.None)
            {
                Debug.WriteLine("\n복호화 처리:");
                Debug.WriteLine($"  복호화 전 데이터 크기: {outData.Length} 바이트");
                JmdEncrypt.DecryptData(key, outData, 0, outData.Length);
                Debug.WriteLine($"  복호화 후 데이터 크기: {outData.Length} 바이트");
            }
            if(dataInfo.BlockProperty == JmdDataInfoProperty.PartialEncrypted)
            {
                JmdDataInfo? secDatainfo = _dataInfoMap.ContainsKey(dataIndex + 1) ? _dataInfoMap[dataIndex + 1] : null;
                if(secDatainfo is not null)
                {
                    Debug.WriteLine("\n부분 암호화 추가 블록 처리:");
                    Debug.WriteLine($"  두 번째 블록 인덱스: 0x{(dataIndex+1):X8}");
                    Debug.WriteLine($"  두 번째 블록 크기: {secDatainfo.DataSize} 바이트");
                    Array.Resize(ref outData, outData.Length + secDatainfo.DataSize);
                    clonedJmdStream.Read(outData, dataInfo.DataSize, secDatainfo.DataSize);
                    Debug.WriteLine($"  최종 데이터 크기: {outData.Length} 바이트");
                }
            }
            return outData;
        }

        private void storeFolderAndFiles(JmdFolder folder, Queue<DataSavingInfo> savingInfo, HashSet<uint> usedIndex, ref int dataOffset, uint outJmdKey)
        {
            if (folder.Name == "" && folder.Parent is not null)
                throw new Exception("folder name couldn't be empty.");
            uint folderDataIndex = folder.getFolderDataIndex();
            while(usedIndex.Contains(folderDataIndex))
                folderDataIndex += 0x5F03E367;
            byte[] folderData;
            
            Queue<DataSavingInfo> fileSavingInfoQueue = new();

            // Encode folder
            using (MemoryStream memStream = new())
            {
                BinaryWriter memWriter = new(memStream);
                IReadOnlyCollection<JmdFolder> subFolders = folder.Folders;
                IReadOnlyCollection<JmdFile> subFiles = folder.Files;
                memWriter.Write(subFolders.Count);
                foreach(JmdFolder subFolder in subFolders)
                {
                    uint subFolderDataIndex = subFolder.getFolderDataIndex();
                    memWriter.WriteNullTerminatedText(subFolder.Name, true);
                    memWriter.Write(subFolderDataIndex);
                }
                memWriter.Write(subFiles.Count);
                foreach (JmdFile subFile in subFiles)
                {
                    if (!subFile.HasDataSource)
                        throw new Exception("data source is null.");
                    
                    uint extNum = subFile.getExtNum();
                    uint fileKey = JmdKey.GetFileKey(outJmdKey, subFile.NameWithoutExt, extNum);
                    int fileSize = subFile.Size;
                    uint fileDataIndex = subFile.getDataIndex(folderDataIndex);
                    byte[] fileData = subFile.GetBytes();
                    uint fileChksum = 0;

                    while (usedIndex.Contains(fileDataIndex) || usedIndex.Contains(fileDataIndex + 1))
                        fileDataIndex += 0x4D21CB4F;
                    
                    if (subFile.FileEncryptionProperty == JmdFileProperty.Encrypted || 
                        subFile.FileEncryptionProperty == JmdFileProperty.CompressedEncrypted)
                    {
                        fileChksum = Adler.Adler32(0, fileData, 0, fileData.Length);
                        Debug.WriteLine($"\n=== 파일 암호화 처리 ===");
                        Debug.WriteLine($"파일: {subFile.Name}");
                        Debug.WriteLine($"암호화 전 데이터 크기: {fileData.Length} 바이트");
                        Debug.WriteLine($"체크섬: 0x{fileChksum:X8}");
                        Debug.WriteLine($"암호화 키: 0x{fileKey:X8}");
                        JmdEncrypt.EncryptData(fileKey, fileData, 0, fileData.Length);
                        Debug.WriteLine($"암호화 후 데이터 크기: {fileData.Length} 바이트");
                    }
                    else if (subFile.FileEncryptionProperty == JmdFileProperty.PartialEncrypted)
                    {
                        Debug.WriteLine($"\n=== 파일 부분 암호화 처리 ===");
                        Debug.WriteLine($"파일: {subFile.Name}");
                        Debug.WriteLine($"전체 데이터 크기: {fileData.Length} 바이트");
                        Debug.WriteLine($"암호화 영역 크기: {Math.Min(0x100, fileData.Length)} 바이트");
                        Debug.WriteLine($"암호화 키: 0x{fileKey:X8}");
                        JmdEncrypt.EncryptData(fileKey, fileData, 0, Math.Min(0x100, fileData.Length));
                        Debug.WriteLine($"암호화 완료");
                    }

                    if (subFile.FileEncryptionProperty == JmdFileProperty.Compressed || 
                        subFile.FileEncryptionProperty == JmdFileProperty.CompressedEncrypted)
                    {
                        Debug.WriteLine($"\n=== 파일 압축 처리 ===");
                        Debug.WriteLine($"파일: {subFile.Name}");
                        Debug.WriteLine($"압축 전 데이터 크기: {fileData.Length} 바이트");
                        using (var ms = new MemoryStream())
                        {
                            using (var compressStream = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionMode.Compress))
                            {
                                compressStream.Write(fileData, 0, fileData.Length);
                            }
                            fileData = ms.ToArray();
                        }
                        Debug.WriteLine($"압축 후 데이터 크기: {fileData.Length} 바이트");
                        Debug.WriteLine($"압축률: {((1.0 - (double)fileData.Length / subFile.Size) * 100):F2}%");
                    }

                    memWriter.WriteNullTerminatedText(subFile.NameWithoutExt, true); 
                    memWriter.Write(extNum);
                    memWriter.Write((int)subFile.FileEncryptionProperty);
                    memWriter.Write(fileDataIndex);
                    memWriter.Write(fileSize);

                    DataSavingInfo fileSavingInfo = new() { File = subFile };
                    if(subFile.FileEncryptionProperty == JmdFileProperty.PartialEncrypted)
                    {
                        fileSavingInfo.Data = new byte[Math.Min(0x100, fileData.Length)];
                        fileSavingInfo.DataInfo.Index = fileDataIndex;
                        fileSavingInfo.DataInfo.BlockProperty = JmdDataInfoProperty.PartialEncrypted;
                        fileSavingInfo.DataInfo.DataSize = fileSavingInfo.Data.Length;
                        fileSavingInfo.DataInfo.UncompressedSize = fileSavingInfo.Data.Length;
                        fileSavingInfo.DataInfo.Checksum = 0;
                        Array.Copy(fileData, 0, fileSavingInfo.Data, 0, fileSavingInfo.Data.Length);
                        usedIndex.Add(fileDataIndex);
                        fileSavingInfoQueue.Enqueue(fileSavingInfo);
                        if (fileData.Length > 0x100)
                        {
                            var dataLength = fileData.Length - 0x100;
                            DataSavingInfo secFileSavingInfo = new() 
                            {
                                Data = new byte[dataLength],
                                DataInfo = 
                                {
                                    Index = fileDataIndex + 1,
                                    BlockProperty = JmdDataInfoProperty.None,
                                    DataSize = dataLength,
                                    UncompressedSize = dataLength,
                                    Checksum = 0
                                }
                            };
                            Array.Copy(fileData, 0x100, secFileSavingInfo.Data, 0, secFileSavingInfo.Data.Length);
                            usedIndex.Add(fileDataIndex + 1);
                            fileSavingInfoQueue.Enqueue(secFileSavingInfo);
                        }
                    }
                    else
                    {
                        fileSavingInfo.Data = fileData;
                        fileSavingInfo.DataInfo.Index = fileDataIndex;
                        fileSavingInfo.DataInfo.Checksum = fileChksum;
                        fileSavingInfo.DataInfo.DataSize = fileData.Length;
                        fileSavingInfo.DataInfo.UncompressedSize = fileSize;
                        switch (subFile.FileEncryptionProperty)
                        {
                            case JmdFileProperty.None:
                                fileSavingInfo.DataInfo.BlockProperty = JmdDataInfoProperty.None;
                                break;
                            case JmdFileProperty.Encrypted:
                                fileSavingInfo.DataInfo.BlockProperty = JmdDataInfoProperty.FullEncrypted;
                                break;
                            case JmdFileProperty.Compressed:
                                fileSavingInfo.DataInfo.BlockProperty = JmdDataInfoProperty.Compressed;
                                break;
                            case JmdFileProperty.CompressedEncrypted:
                                fileSavingInfo.DataInfo.BlockProperty = JmdDataInfoProperty.CompressedEncrypted;
                                break;
                        }
                        usedIndex.Add(fileDataIndex);
                        fileSavingInfoQueue.Enqueue(fileSavingInfo);
                    }

                }
                folderData = memStream.ToArray();
            }

            uint folderDataDecChksum = Adler.Adler32(0, folderData, 0, folderData.Length);
            uint folderKey = JmdKey.GetDirectoryDataKey(outJmdKey);
            JmdEncrypt.EncryptData(folderKey, folderData, 0, folderData.Length);

            DataSavingInfo folderSavingInfo = new();
            folderSavingInfo.Data = folderData;
            folderSavingInfo.DataInfo.Offset = dataOffset;
            folderSavingInfo.DataInfo.Index = folderDataIndex;
            folderSavingInfo.DataInfo.Checksum = folderDataDecChksum;
            folderSavingInfo.DataInfo.DataSize = folderData.Length;
            folderSavingInfo.DataInfo.UncompressedSize = folderData.Length;
            folderSavingInfo.DataInfo.BlockProperty = JmdDataInfoProperty.FullEncrypted;
            usedIndex.Add(folderDataIndex);
            savingInfo.Enqueue(folderSavingInfo);
            dataOffset = (dataOffset + folderSavingInfo.DataInfo.DataSize + 0xFF) & 0x7FFFFF00;
            foreach (JmdFolder subFolder in folder.Folders)
                storeFolderAndFiles(subFolder, savingInfo, usedIndex, ref dataOffset, outJmdKey);
            while(fileSavingInfoQueue.Count > 0)
            {
                DataSavingInfo fileSavingInfo = fileSavingInfoQueue.Dequeue();
                fileSavingInfo.DataInfo.Offset = dataOffset;
                savingInfo.Enqueue(fileSavingInfo);
                dataOffset = (dataOffset + fileSavingInfo.DataInfo.DataSize + 0xFF) & 0x7FFFFF00;
            }
        }
        
        private void releaseAllHandlers()
        {
            foreach(JmdFileHandler handler in _fileHandlers.Values)
                handler.releaseHandler();
            _fileHandlers.Clear();
        }

        private unsafe void generateDataInfoKey(byte[] outKeyBuffer)
        {
            Random random = new();
            fixed(byte* bPtr = outKeyBuffer)
            {
                for(int i = 0; i < 24; i++)
                {
                    ulong rndNum = (ulong)random.NextInt64();
                    ulong* ulPtr = (ulong*)(bPtr + i);
                    *ulPtr ^= rndNum;
                }
                for(int i = 24; i < 32; i++)
                {
                    ulong rndNum = (ulong)random.NextInt64(); 
                    BitOperations.RotateLeft(rndNum, (i * 0x1587E329) & 0x1F);
                    bPtr[i] ^= (byte)(rndNum & 0xFF);
                }
            }
        }
        #endregion

        #region Structs
        private class DataSavingInfo
        {
            public JmdDataInfo DataInfo = new();
            public JmdFile? File;
            public byte[] Data = [];
        }
        #endregion
    }

    // Static
    public partial class JmdArchive
    {
        #region Constants
        public readonly string[] RhLayerIdentifiers = ["J2m Data Format 1.0"];
        public const string RhLayerSecondText = "j2m & raycity flighting!!";
        #endregion
    }
}
