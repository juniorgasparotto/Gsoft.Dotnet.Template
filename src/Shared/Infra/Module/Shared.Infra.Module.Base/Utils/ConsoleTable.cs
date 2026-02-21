namespace Shared.Infra.Module.Base.Utils;

/// <summary>
/// Renders ASCII tables to the console with window-adaptive column widths.
/// Text is truncated with "..." when it exceeds column width (no line wrapping).
/// </summary>
public static class ConsoleTable
{
    private const int MinTotalWidth = 50;
    private const int DefaultWidth = 80;

    /// <summary>
    /// Renders a table with flexible column widths based on terminal window size.
    /// </summary>
    /// <param name="title">Header text displayed above the table (e.g. "[Modules] 5 loaded")</param>
    /// <param name="headers">Column header names</param>
    /// <param name="rows">Row data. Each row is an array of cell values (one per column).</param>
    /// <param name="columnRatios">Optional. Width ratio per column (0.0-1.0). Sum should be 1.0. If null, columns are evenly distributed.</param>
    /// <param name="rowColors">Optional. ANSI color code for each row (cycled if fewer than row count).</param>
    public static void Render(
        string title,
        string[] headers,
        IEnumerable<string[]> rows,
        double[]? columnRatios = null,
        string[]? rowColors = null)
    {
        var rowList = rows.ToList();
        var totalWidth = GetAvailableWidth();

        var colCount = headers.Length;
        var ratios = columnRatios ?? Enumerable.Repeat(1.0 / colCount, colCount).ToArray();

        // Reserve space: "  | " + (n-1)*" | " + " |"  =>  2+1+1 + 3*(n-1) + 2  =  3n+3
        var borderOverhead = 3 * colCount + 3;
        var usableWidth = Math.Max(totalWidth - borderOverhead, MinTotalWidth);

        var colWidths = ratios
            .Select(r => Math.Max(4, (int)(usableWidth * r)))
            .ToArray();

        // Content-aware: shrink col0 if it exceeds max content length
        var maxCol0Content = Math.Max(
            headers[0].Length,
            rowList.Count > 0 ? rowList.Max(r => (r.Length > 0 ? r[0] ?? "" : "").Length) : 0);
        if (colWidths.Length > 0 && colWidths[0] > maxCol0Content + 2)
        {
            var excess = colWidths[0] - (maxCol0Content + 2);
            colWidths[0] = maxCol0Content + 2;
            if (colWidths.Length > 1)
                colWidths[1] = Math.Max(20, colWidths[1] + excess);
        }

        // Ensure we use exactly usableWidth
        var diff = usableWidth - colWidths.Sum();
        if (diff != 0 && colWidths.Length > 0)
            colWidths[^1] = Math.Max(4, colWidths[^1] + diff);

        var bulletColors = rowColors ?? AnsiColors.RowColors;
        var sep = BuildSeparator(colWidths);

        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine();
        Console.WriteLine(sep);
        Console.WriteLine(BuildHeaderRow(headers, colWidths));
        Console.WriteLine(sep);

        var lineIndex = 0;
        foreach (var row in rowList)
        {
            var parts = new List<string>();
            for (var c = 0; c < colCount; c++)
            {
                var cell = c < row.Length ? row[c] ?? "" : "";
                var truncated = Truncate(cell, colWidths[c]);
                var padded = truncated.PadRight(colWidths[c]);
                var styled = (c == 0 && !string.IsNullOrEmpty(truncated))
                    ? $"{AnsiColors.Bold}{bulletColors[lineIndex % bulletColors.Length]}{padded}{AnsiColors.Reset}"
                    : $"{AnsiColors.Gray}{padded}{AnsiColors.Reset}";
                parts.Add(styled);
            }
            Console.WriteLine($"  {AnsiColors.Gray}|{AnsiColors.Reset} {string.Join($" {AnsiColors.Gray}|{AnsiColors.Reset} ", parts)} {AnsiColors.Gray}|{AnsiColors.Reset}");
            lineIndex++;
        }

        Console.WriteLine(sep);
        Console.WriteLine();
    }

    private static int GetAvailableWidth()
    {
        if (Console.IsOutputRedirected)
            return DefaultWidth;

        try
        {
            var w = Console.WindowWidth;
            if (w >= MinTotalWidth && w <= 500) return w;
        }
        catch { /* web/service: no console, stdout redirected */ }
        return DefaultWidth;
    }

    private static string BuildSeparator(int[] colWidths)
    {
        var parts = colWidths.Select(w => new string('-', w + 2));
        return $"  {AnsiColors.Gray}+{string.Join("+", parts)}+{AnsiColors.Reset}";
    }

    private static string BuildHeaderRow(string[] headers, int[] colWidths)
    {
        var parts = headers
            .Select((h, i) => (h.PadRight(colWidths[i]), i))
            .Select(x => $"{AnsiColors.Dim}{x.Item1}{AnsiColors.Reset}");
        return $"  {AnsiColors.Gray}|{AnsiColors.Reset} {string.Join($" {AnsiColors.Gray}|{AnsiColors.Reset} ", parts)} {AnsiColors.Gray}|{AnsiColors.Reset}";
    }

    private static string Truncate(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return text ?? "";
        if (text.Length <= maxWidth) return text;
        return maxWidth <= 3 ? text[..maxWidth] : $"{text[..(maxWidth - 3)]}...";
    }
}
