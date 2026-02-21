namespace Shared.UI.Api.Base;

using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using Shared.Infra.Module.Base.Utils;
using System.Reflection;

/// <summary>
/// Provides functionality to list and display information about loaded modules.
/// </summary>
public static class StartupInfo
{
    /// <summary>
    /// Lists all loaded modules with their descriptions.
    /// </summary>
    public static void ListLoadedModules(IEnumerable<IWebModule> modules)
    {
        var moduleList = modules.ToList();
        var count = moduleList.Count;

        if (count == 0)
        {
            Console.WriteLine();
            Console.WriteLine($"{AnsiColors.Gray}[Modules]{AnsiColors.Reset} {AnsiColors.Dim}None loaded{AnsiColors.Reset}");
            Console.WriteLine();
            return;
        }

        var title = $"{AnsiColors.Bold}{AnsiColors.Cyan}[Modules]{AnsiColors.Reset} {AnsiColors.Bold}{count}{AnsiColors.Reset} loaded";
        var headers = new[] { "Module", "Description" };
        var rows = moduleList.Select(m =>
        {
            var moduleType = m.GetType();
            var attr = moduleType.GetCustomAttribute<ModuleAttribute>(inherit: false);
            var moduleTitle = attr?.Title ?? moduleType.Name;
            var description = attr?.Description ?? "(no description)";
            return new[] { moduleTitle, description };
        }).ToList();

        // Module column ~25%, Description ~75%
        ConsoleTable.Render(title, headers, rows, columnRatios: [0.25, 0.75]);
    }
}
