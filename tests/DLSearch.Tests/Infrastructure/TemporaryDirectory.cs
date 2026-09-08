using System.Text;

namespace DLSearch.Tests.Infrastructure;

public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dlsearch-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    public string CreateDirectory(string relativePath)
    {
        string full = Combine(relativePath);
        Directory.CreateDirectory(full);
        return full;
    }

    public string WriteBytes(string relativePath, byte[] content)
    {
        string full = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
        return full;
    }

    public string WriteUtf8(string relativePath, string content) => WriteBytes(relativePath, Encoding.UTF8.GetBytes(content));

    public string WriteEncoded(string relativePath, string content, Encoding encoding)
    {
        byte[] preamble = encoding.GetPreamble();
        byte[] body = encoding.GetBytes(content);
        byte[] bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);
        return WriteBytes(relativePath, bytes);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
