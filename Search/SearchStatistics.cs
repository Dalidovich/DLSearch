namespace DLSearch.Search;

public sealed class SearchStatistics
{
    private long _directories;
    private long _filesSeen;
    private long _filesRead;
    private long _bytesRead;
    private long _skippedBySize;
    private long _skippedBinary;
    private long _skippedDenied;
    private long _nameMatches;
    private long _contentMatches;
    private long _contentFiles;
    private long _filesProcessed;
    private long _totalFiles = -1;

    public long Directories => Interlocked.Read(ref _directories);

    public long FilesSeen => Interlocked.Read(ref _filesSeen);

    public long FilesRead => Interlocked.Read(ref _filesRead);

    public long BytesRead => Interlocked.Read(ref _bytesRead);

    public long SkippedBySize => Interlocked.Read(ref _skippedBySize);

    public long SkippedBinary => Interlocked.Read(ref _skippedBinary);

    public long SkippedDenied => Interlocked.Read(ref _skippedDenied);

    public long NameMatches => Interlocked.Read(ref _nameMatches);

    public long ContentMatches => Interlocked.Read(ref _contentMatches);

    public long ContentFiles => Interlocked.Read(ref _contentFiles);

    public long FilesProcessed => Interlocked.Read(ref _filesProcessed);

    public long TotalFiles => Interlocked.Read(ref _totalFiles);

    public long SkippedTotal => SkippedBySize + SkippedBinary + SkippedDenied;

    public bool Interrupted { get; set; }

    public void CountDirectory() => Interlocked.Increment(ref _directories);

    public void CountFile() => Interlocked.Increment(ref _filesSeen);

    public void CountNameMatch() => Interlocked.Increment(ref _nameMatches);

    public void CountRead(long bytes)
    {
        Interlocked.Increment(ref _filesRead);
        Interlocked.Add(ref _bytesRead, bytes);
    }

    public void CountSkipped(SkipReason reason)
    {
        switch (reason)
        {
            case SkipReason.Size:
                Interlocked.Increment(ref _skippedBySize);
                break;
            case SkipReason.Binary:
                Interlocked.Increment(ref _skippedBinary);
                break;
            case SkipReason.Denied:
                Interlocked.Increment(ref _skippedDenied);
                break;
        }
    }

    public void CountDenied() => Interlocked.Increment(ref _skippedDenied);

    public void CountContentFile(int matches)
    {
        Interlocked.Increment(ref _contentFiles);
        Interlocked.Add(ref _contentMatches, matches);
    }

    public void CountProcessed() => Interlocked.Increment(ref _filesProcessed);

    public void FreezeTotalFiles() => Interlocked.Exchange(ref _totalFiles, FilesSeen);
}
