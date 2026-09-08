namespace DLSearch.Output;

public static class Ansi
{
    private static readonly string Escape = ((char)27).ToString();

    public static readonly string Reset = Escape + "[0m";
    public static readonly string Header = Escape + "[96m";
    public static readonly string Directory = Escape + "[90m";
    public static readonly string FileName = Escape + "[97m";
    public static readonly string LineNumber = Escape + "[90m";
    public static readonly string Highlight = Escape + "[30;103m";
    public static readonly string Error = Escape + "[91m";
    public static readonly string Muted = Escape + "[90m";
    public static readonly string EraseLine = "\r" + Escape + "[2K";
}
