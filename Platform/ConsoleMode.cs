using System.Runtime.InteropServices;
using System.Text;

namespace DLSearch.Platform;

public static partial class ConsoleMode
{
    private const int StandardOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    public static void Prepare()
    {
        EnableUtf8();
        EnableVirtualTerminal();
    }

    private static void EnableUtf8()
    {
        try
        {
            Console.OutputEncoding = new UTF8Encoding(false);
        }
        catch (IOException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    private static void EnableVirtualTerminal()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        nint handle = GetStdHandle(StandardOutputHandle);
        if (handle == 0 || handle == -1)
        {
            return;
        }

        if (GetConsoleMode(handle, out uint mode))
        {
            SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
