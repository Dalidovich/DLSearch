using System.Text;

namespace DLSearch.Output;

public sealed class ConsoleSurface
{
    private const int FallbackWidth = 120;
    private const int MinimumWidth = 40;

    private readonly Lock _gate = new();
    private readonly TextWriter _writer = Console.Out;

    private string _progress = string.Empty;

    public ConsoleSurface()
    {
        Decorated = !Console.IsOutputRedirected;
    }

    public bool Decorated { get; }

    public int Width
    {
        get
        {
            if (!Decorated)
            {
                return FallbackWidth;
            }

            try
            {
                return Math.Max(MinimumWidth, Console.WindowWidth - 1);
            }
            catch (IOException)
            {
                return FallbackWidth;
            }
        }
    }

    public void Write(string block)
    {
        lock (_gate)
        {
            var builder = new StringBuilder(block.Length + 16);

            if (_progress.Length > 0)
            {
                builder.Append(Ansi.EraseLine);
            }

            builder.Append(block);

            if (!block.EndsWith('\n'))
            {
                builder.Append('\n');
            }

            if (_progress.Length > 0)
            {
                builder.Append(_progress);
            }

            _writer.Write(builder.ToString());
            _writer.Flush();
        }
    }

    public void SetProgress(string text)
    {
        if (!Decorated)
        {
            return;
        }

        lock (_gate)
        {
            _progress = text;
            _writer.Write(Ansi.EraseLine);
            _writer.Write(text);
            _writer.Flush();
        }
    }

    public void ClearProgress()
    {
        if (!Decorated)
        {
            return;
        }

        lock (_gate)
        {
            if (_progress.Length == 0)
            {
                return;
            }

            _progress = string.Empty;
            _writer.Write(Ansi.EraseLine);
            _writer.Flush();
        }
    }
}
