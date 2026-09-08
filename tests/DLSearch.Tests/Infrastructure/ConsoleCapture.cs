namespace DLSearch.Tests.Infrastructure;

public sealed class ConsoleCapture : IDisposable
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly TextWriter _original;
    private readonly StringWriter _writer = new();

    public ConsoleCapture()
    {
        Gate.Wait();
        _original = Console.Out;
        Console.SetOut(_writer);
    }

    public string Text => AnsiText.Strip(_writer.ToString());

    public void Dispose()
    {
        Console.SetOut(_original);
        Gate.Release();
    }
}
