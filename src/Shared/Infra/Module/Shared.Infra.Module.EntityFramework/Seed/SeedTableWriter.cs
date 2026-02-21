namespace Shared.Infra.Module.EntityFramework.Seed;

using Shared.Infra.Module.Base.Utils;

/// <summary>
/// Displays seed results in table format (uses ConsoleTable, same as module initialization).
/// </summary>
public static class SeedTableWriter
{

    /// <summary>
    /// Writes the seed results table to the Console.
    /// </summary>
    public static void Write(IReadOnlyList<SeedResult> results, IReadOnlyList<SeedError> errors)
    {
        if (results.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine($"{AnsiColors.Gray}[Seed]{AnsiColors.Reset} {AnsiColors.Dim}No files processed{AnsiColors.Reset}");
            Console.WriteLine();
            return;
        }

        var headers = new[] { "Context", "Entity", "File", "R", "I", "U", "D", "Error" };
        var rows = results.Select(r => new[]
        {
            r.Context,
            r.Entity,
            TruncatePathStart(r.File, 40),
            r.Error is not null ? "-" : r.Read.ToString(),
            r.Error is not null ? "-" : r.Inserted.ToString(),
            (r.Error is not null || r.Deleted > 0) ? "-" : r.Updated.ToString(),
            r.Deleted > 0 ? r.Deleted.ToString() : "-",
            r.Error ?? "-"
        }).ToArray();

        ConsoleTable.Render(
            $"{AnsiColors.Bold}{AnsiColors.Cyan}[Seed]{AnsiColors.Reset} {AnsiColors.Bold}{results.Count}{AnsiColors.Reset} row(s) processed",
            headers,
            rows,
            columnRatios: [0.12, 0.12, 0.22, 0.06, 0.08, 0.06, 0.06, 0.28],
            rowColors: AnsiColors.RowColors);

        var errorResults = results.Where(r => r.Error is not null).ToList();
        if (errorResults.Count > 0)
        {
            Console.WriteLine($"{AnsiColors.Yellow}[Seed Errors]{AnsiColors.Reset} {AnsiColors.Bold}{errorResults.Count}{AnsiColors.Reset} error(s):");
            Console.WriteLine();
            foreach (var r in errorResults)
            {
                Console.WriteLine($"  {AnsiColors.Yellow}{TruncatePathStart(r.File, 50)}{AnsiColors.Reset}" + (r.Entity != "-" ? $" ({r.Entity})" : ""));
                Console.WriteLine($"    {AnsiColors.Gray}{r.Error}{AnsiColors.Reset}");
                Console.WriteLine();
            }
        }
    }

    private static string TruncatePathStart(string path, int maxLen)
    {
        if (path.Length <= maxLen) return path;
        return "..." + path[^(maxLen - 3)..];
    }
}
