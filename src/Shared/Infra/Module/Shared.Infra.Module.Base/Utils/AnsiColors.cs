namespace Shared.Infra.Module.Base.Utils;

/// <summary>
/// ANSI escape codes for console output.
/// </summary>
public static class AnsiColors
{
    public const string Reset = "\u001b[0m";
    public const string Bold = "\u001b[1m";
    public const string Dim = "\u001b[2m";
    public const string Gray = "\u001b[90m";
    public const string Green = "\u001b[32m";
    public const string Yellow = "\u001b[33m";
    public const string Cyan = "\u001b[36m";
    public const string Magenta = "\u001b[35m";

    /// <summary>
    /// Default row colors for tables: Cyan, Green, Yellow, Magenta.
    /// </summary>
    public static readonly string[] RowColors = [Cyan, Green, Yellow, Magenta];
}
