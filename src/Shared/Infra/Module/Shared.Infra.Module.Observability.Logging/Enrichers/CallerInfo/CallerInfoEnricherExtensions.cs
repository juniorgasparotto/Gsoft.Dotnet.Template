using System.Reflection;
using Serilog;
using Serilog.Configuration;

namespace Shared.Infra.Module.Observability.Logging.Enrichers.CallerInfo;

public static class CallerInfoEnricherExtensions
{
    /// <summary>
    /// Enrich log events with information about the calling method.
    /// </summary>
    /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
    /// <param name="includeFileInfo">Whether to include the caller's file information (file name, line number, column number).</param>
    /// <param name="allowedAssemblies">Which assemblies to consider when finding the calling method in the stack trace.</param>
    /// <param name="prefix">An optional prefix to prepend to all property values.</param>
    /// <param name="filePathDepth">The number of parent directories to include in the file name. If zero or negative, the full path is returned. If larger than the actual depth of the file, the full path is also returned.</param>
    /// <returns>The modified logger configuration.</returns>
    public static LoggerConfiguration WithCallerInfo(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        bool includeFileInfo,
        IEnumerable<string> allowedAssemblies,
        string prefix = "",
        int filePathDepth = 0)
    {
        return enrichmentConfiguration.With(new CallerInfoEnricher(includeFileInfo, allowedAssemblies, prefix, filePathDepth));
    }

    /// <summary>
    /// Enrich log events with information about the calling method. If using from appsettings.json, also provide the startingAssemblies as it will otherwise be a Serilog assembly that is inferred as the calling assembly.
    /// </summary>
    /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
    /// <param name="includeFileInfo">Whether to include the caller's file information (file name, line number, column number).</param>
    /// <param name="assemblyPrefix">The prefix of assemblies to allow when finding the calling method in the stack trace.</param>
    /// <param name="prefix">An optional prefix to prepend to all property values.</param>
    /// <param name="startingAssembly">The optional name of the assembly from which to discover other related ones with the given prefix. If not provided, the calling assembly of this method is used as the starting point.</param>
    /// <param name="filePathDepth">The number of parent directories to include in the file name. If zero or negative, the full path is returned. If larger than the actual depth of the file, the full path is also returned.</param>
    /// <param name="excludedPrefixes">Which assembly prefixes to exclude when finding the calling method in the stack trace.</param>
    /// <param name="includedPrefixes">Which assembly prefixes to include. When set, only assemblies whose name starts with one of these are considered (overrides assemblyPrefix).</param>
    /// <returns>The modified logger configuration.</returns>
    public static LoggerConfiguration WithCallerInfo(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        bool includeFileInfo = true,
        string assemblyPrefix = "",
        string prefix = "",
        string startingAssembly = "",
        int filePathDepth = 0,
        IEnumerable<string>? excludedPrefixes = null,
        IEnumerable<string>? includedPrefixes = null)
    {
        var startingAssemblies = new Stack<Assembly>();

        if (string.IsNullOrWhiteSpace(startingAssembly))
        {
            startingAssemblies.Push(Assembly.GetCallingAssembly());
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                startingAssemblies.Push(entryAssembly);
            }
        }
        else
        {
            startingAssemblies.Push(Assembly.Load(startingAssembly));
        }

        var referencedAssemblies = GetAssemblies(
            startingAssemblies,
            asm => IsAssemblyIncludedByPrefix(asm, assemblyPrefix, includedPrefixes),
            asm => excludedPrefixes?.Any(excluded => asm?.Name?.StartsWith(excluded, StringComparison.OrdinalIgnoreCase) ?? false) ?? false);

        return enrichmentConfiguration.WithCallerInfo(includeFileInfo, referencedAssemblies, prefix, filePathDepth);
    }

    /// <summary>
    /// Find the assemblies that a starting Assembly references, filtering with some predicate.
    /// Adapted from https://stackoverflow.com/a/10253634/2102106
    /// </summary>
    /// <param name="startingAssemblies">The starting assembly.</param>
    /// <param name="filter">A filtering predicate based on the AssemblyName</param>
    /// <param name="exclude">An exclusion predicate based on the AssemblyName</param>
    /// <returns>The list of referenced Assembly names</returns>
    private static IEnumerable<string> GetAssemblies(Stack<Assembly> startingAssemblies, Func<AssemblyName, bool> filter, Func<AssemblyName, bool>? exclude = null)
    {
        var asmNames = new HashSet<string>(comparer: StringComparer.OrdinalIgnoreCase);

        do
        {
            var asm = startingAssemblies.Pop();

            if (!AssemblyExistsInList(asmNames, asm.GetName()) && IsAssemblyIncluded(filter, asm.GetName()) && !IsAssemblyExcluded(exclude, asm.GetName()))
            {
                asmNames.Add(asm.GetName().Name!);
            }

            foreach (var reference in asm.GetReferencedAssemblies())
            {
                try
                {
                    if (IsAssemblyExcluded(exclude, asm.GetName()))
                        continue;

                    var referenceAsm = Assembly.Load(reference);

                    if (AssemblyExistsInList(asmNames, referenceAsm.GetName()) || !IsAssemblyIncluded(filter, referenceAsm.GetName()) || IsAssemblyExcluded(exclude, referenceAsm.GetName()))
                    {
                        continue;
                    }

                    startingAssemblies.Push(referenceAsm);
                    asmNames.Add(reference.Name!);
                }
                catch
                {
                    // Assembly não pôde ser carregado, ignora
                }
            }
        } while (startingAssemblies.Count > 0);

        return asmNames;
    }

    private static bool IsAssemblyIncludedByPrefix(AssemblyName asm, string assemblyPrefix, IEnumerable<string>? includedPrefixes)
    {
        var name = asm.Name;
        if (string.IsNullOrEmpty(name)) return false;

        var prefixes = includedPrefixes?.Where(p => !string.IsNullOrEmpty(p)).ToList();
        if (prefixes is { Count: > 0 })
            return prefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrEmpty(assemblyPrefix) || name.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAssemblyExcluded(Func<AssemblyName, bool>? exclude, AssemblyName asm)
    {
        return exclude != null && exclude(asm);
    }

    private static bool IsAssemblyIncluded(Func<AssemblyName, bool>? include, AssemblyName asm)
    {
        return include != null && include(asm);
    }

    private static bool AssemblyExistsInList(IEnumerable<string> asmNames, AssemblyName asm)
    {
        return asmNames.Contains(asm.Name);
    }
}
