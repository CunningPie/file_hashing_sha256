using System.Diagnostics;
using System.Text;
using file_hashing_sha256;

var sw = new Stopwatch();


var test1 =
    @"D:\GitHub\file_hashing_sha256\file_hashing_sha256\file_hashing_sha256_tests\tests_data\simple_text_file.txt";
var test2 =
    @"D:\GitHub\file_hashing_sha256\file_hashing_sha256\file_hashing_sha256_tests\tests_data\tolstoy.txt";
var test3 =
    @"D:\GitHub\file_hashing_sha256\file_hashing_sha256\file_hashing_sha256_tests\tests_data\simple_binary_file.txt";
var test4 =
    @"D:\GitHub\file_hashing_sha256\file_hashing_sha256\file_hashing_sha256_tests\tests_data\dlc1.rpkg";

var fileName = Console.ReadLine();
int segmentsCount;

if (!int.TryParse(Console.ReadLine(), out segmentsCount))
{
    throw new ArgumentException("Invalid number of segments!");
}

if (fileName == null)
{
    throw new ArgumentNullException();
}

var fileInfo = new FileInfo(fileName);

if (fileInfo.Length < segmentsCount)
{
    throw new ArgumentException("Invalid number of segments! Can't split file!");
}

var SHA256SH = new SHA256SegmentHashing(fileInfo.Length, segmentsCount);
var SHA256SHP = new SHA256SegmentHashingParallel(fileInfo.Length, segmentsCount);
var cts = new CancellationTokenSource();
var token = cts.Token;


string HashToString(byte[]? hash)
{
    if (hash == null)
    {
        throw new NullReferenceException();
    }
    
    var sb = new StringBuilder();
    
    foreach (var b in hash)
    {
        sb.Append(b.ToString(("x2")));
    }

    return sb.ToString();
}

void PrintSegmentsHashes(byte[]?[] hashes)
{
    if (hashes == null)
    {
        throw new ArgumentNullException();
    }
    var i = 1;
    
    foreach (var hash in hashes)
    {
        Console.WriteLine($"Segment {i++}: {HashToString(hash)}");
    }
}

using (var stream = fileInfo.Open(FileMode.Open))
{
    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
    {
        sw.Start();
        
        Task.Factory.StartNew(() =>
        {
            Console.WriteLine("Press 'q' to abort calculation...");
            if (Console.ReadKey().KeyChar != 'q') return;
            cts.Cancel();
            Console.Clear();
        });
        
        var hashesParallel = SHA256SHP.CalculateSHA256HashesParallel(reader, token);
        
        sw.Stop();
        Console.WriteLine($"Time: {sw.ElapsedMilliseconds}");
        
        try
        {
            PrintSegmentsHashes(hashesParallel);
        }
        catch (NullReferenceException)
        {
            Console.Write("Invalid operation!");
        }


        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        sw.Reset();
        sw.Start();
        var hashes = SHA256SH.CalculateSHA256Hashes(reader);
        sw.Stop();
        Console.WriteLine($"Time: {sw.ElapsedMilliseconds}");
        PrintSegmentsHashes(hashes);
    }
}

