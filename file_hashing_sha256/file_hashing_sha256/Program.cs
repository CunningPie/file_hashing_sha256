using System.Text;
using file_hashing_sha256;

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

var SHA256SHP = new SHA256SegmentHashingParallel(fileInfo.Length, segmentsCount);
var cts = new CancellationTokenSource();
var token = cts.Token;


string HashToString(byte[]? hash)
{
    if (hash == null)
    {
        throw new ArgumentNullException();
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
        Task.Factory.StartNew(() =>
        {
            Console.WriteLine("Press 'q' to abort calculation...");
            if (Console.ReadKey().KeyChar != 'q') return;
            cts.Cancel();

        });
        
        var hashesParallel = SHA256SHP.CalculateSHA256HashesParallel(reader, token);
        
        try
        {
            if (!cts.IsCancellationRequested)
            {
                PrintSegmentsHashes(hashesParallel);
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Calculations aborted..");
            }
        }
        catch (ArgumentNullException)
        {
            Console.Write("Invalid operation!");
        }
    }
}

