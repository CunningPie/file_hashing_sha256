using System.Security.Cryptography;

namespace file_hashing_sha256;


/// <summary>
/// Последовательно вычисляет хеши сегментов файлов с помощью SHA256. (Данный класс нужен лишь для сравнения результатов с параллельной версией)
/// </summary>
public class SHA256SegmentHashing
{
    private int _progress = 0;
    private int defaultChunkSize = 8 * 1024 * 1024 * 16;
    
    private int ChunkSize { get; }
    private long SegmentSize { get; }
    private long FileSize { get; }
    private int SegmentsCount { get; }
    
    public SHA256SegmentHashing(long fileSize, int segmentsCount)
    {
        SegmentSize = fileSize / segmentsCount;
        FileSize = fileSize;
        ChunkSize = SegmentSize > defaultChunkSize ? defaultChunkSize : (int)SegmentSize;
        SegmentsCount = segmentsCount;
    }

    public byte[][] CalculateSHA256Hashes(BinaryReader reader)
    {
        var hashes = new byte[SegmentsCount][];
        
        for (var i = 0; i < SegmentsCount; i++)
        {
            using (var sha = SHA256.Create())
            {
                var chunkOffset = 0L;

                while ((chunkOffset + 1) * ChunkSize <= SegmentSize)
                {
                    var buff = reader.ReadBytes(ChunkSize);
                    sha.TransformBlock(buff, 0, ChunkSize, buff, 0);
                    chunkOffset += 1;
                }

                var remainingBytesCount = (int) (i < SegmentsCount - 1
                    ? (SegmentSize - ChunkSize * chunkOffset)
                    : (FileSize - SegmentSize * SegmentsCount));

                var remainingBuff = reader.ReadBytes(remainingBytesCount);

                sha.TransformFinalBlock(remainingBuff, 0, remainingBytesCount);

                hashes[i] = sha.Hash;
                Console.Clear();
                Console.WriteLine($"Progress: {++_progress * 100 / SegmentsCount}%");
            }
        }
                
        return hashes;
    }
}