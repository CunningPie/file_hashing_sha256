using System.Security.Cryptography;

namespace file_hashing_sha256;

/// <summary>
/// Параллельно вычисляет хеши сегментов файла с помощью SHA256.
/// (Распараллеливание происходит при расчете хешей, чтение из файла блокируется каждым потоком при считывании нового куска памяти,
/// поэтому наибольшая эффективность достигается когда вычисление хеша значительно дольше, чем чтение из файла)
/// </summary>
public class SHA256SegmentHashingParallel
{
    private object _lockObj;
    private int _progress = 0;

    /// <summary>
    /// Размер куска памяти для случая, когда размер сегмента достаточно велик (16 Мб, подобран эмпирически)
    /// </summary>
    private const int DefaultChunkSize = 8 * 1024 * 1024 * 16;

    /// <summary>
    /// Размер куска памяти, считываемого из файла за раз.
    /// </summary>
    private int ChunkSize { get; }
    
    /// <summary>
    /// Размер сегмента файла.
    /// </summary>
    private long SegmentSize { get; }
    private long FileSize { get; }
    private int SegmentsCount { get; }
    
    
    /// <param name="fileSize"> Размер файла. </param>
    /// <param name="segmentsCount"> Количество сегментов. </param>
    public SHA256SegmentHashingParallel(long fileSize, int segmentsCount)
    {
        _lockObj = new object();
        SegmentSize = fileSize / segmentsCount;
        FileSize = fileSize;
        ChunkSize = SegmentSize > DefaultChunkSize ? DefaultChunkSize : (int)SegmentSize;
        SegmentsCount = segmentsCount;
    }
    
    /// <summary>
    /// Проверяет, выполнены ли все Task в массиве tasks.
    /// </summary>
    private bool CheckTasksCompletion(Task[] tasks)
    {
        if (tasks == null)
        {
            throw new NullReferenceException();
        }

        return tasks.All(task => task.IsCompleted);
    }
    
    /// <summary>
    /// Вычисляет SHA256 хеш сегмента файла.
    /// </summary>
    /// <param name="reader">Поток данных.</param>
    /// <param name="segmentNumber">Номер сегмента.</param>
    /// <param name="token">Токен для прекращения работы в случае отмены.</param>
    /// <returns>Хеш одного сегмента файла.</returns>
    private byte[]? GetSegmentHash(BinaryReader reader, int segmentNumber, CancellationToken token)
    {
        using (var sha = SHA256.Create())
        {
            var chunkOffset = 0L;

            while ((long) (chunkOffset + 1) * ChunkSize <= SegmentSize)
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                var buff = new byte[ChunkSize];

                lock (_lockObj)
                {
                    reader.BaseStream.Seek(segmentNumber * SegmentSize + chunkOffset * ChunkSize, SeekOrigin.Begin);
                    buff = reader.ReadBytes(ChunkSize);
                }

                sha.TransformBlock(buff, 0, ChunkSize, buff, 0);
                chunkOffset += 1;
            }

            var remainingBytesCount = (int) (segmentNumber < SegmentsCount - 1
                ? (SegmentSize - ChunkSize * chunkOffset)
                : (FileSize - SegmentSize * SegmentsCount));
            var remainingBuff = new byte[remainingBytesCount];

            lock (_lockObj)
            {
                reader.BaseStream.Seek(segmentNumber * SegmentSize + chunkOffset * ChunkSize, SeekOrigin.Begin);
                remainingBuff = reader.ReadBytes(remainingBytesCount);
            }

            sha.TransformFinalBlock(remainingBuff, 0, remainingBytesCount);

            lock (_lockObj)
            {
                Console.Clear();
                Console.WriteLine($"Progress: {++_progress * 100 / SegmentsCount}%");
            }

            return sha.Hash;
        }
    }
    
    
    /// <summary>
    /// Создает массив Task для каждого сегмента файла и вычисляет хеш каждого из них с использованием SHA256.
    /// </summary>
    /// <param name="reader"> Поток данных. </param>
    /// <param name="token"> Токен для отмены Task.</param>
    /// <returns> Массив хешей всех сегментов. </returns>
    /// <exception cref="NullReferenceException"></exception>
    public byte[]?[] CalculateSHA256HashesParallel(BinaryReader reader, CancellationToken token)
    {
        if (reader == null)
        {
            throw new NullReferenceException();
        }
        
        byte[]?[] hashes = new byte[SegmentsCount][];
        var tasks = new Task[SegmentsCount];

        for (var i = 0; i < SegmentsCount; i++)
        {
            var segment = i;
            tasks[i] = Task.Factory.StartNew(() => hashes[segment] = GetSegmentHash(reader, segment, token), token);
        }
        
        while (!CheckTasksCompletion(tasks))
        {
        }
        
        return hashes;
    }
}

