namespace Shared.UI.Api.Base.Bootstrap;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using System.Reflection;

/// <summary>
/// Classe responsável por criar e resolver módulos usando um ServiceCollection bootstrap.
/// </summary>
internal static class ModuleResolver
{
    /// <summary>
    /// Cria um ServiceCollection bootstrap para resolver dependências dos módulos.
    /// </summary>
    public static IEnumerable<IWebModule> CreateModules(
        WebApplicationBuilder builder,
        ServiceProvider bootstrapProvider,
        IEnumerable<object> modules)
    {

        var ret = new List<IWebModule>();

        // Resolver módulos por tipo usando ServiceProvider
        foreach (var moduleType in modules)
        {
            IWebModule? webModule = null;
            if (moduleType is IWebModule webModuleCast)
                webModule = webModuleCast;
            else if (moduleType is Type type)
                webModule = bootstrapProvider.GetRequiredService(type) as IWebModule;

            if (webModule != null)
            {
                var t = webModule.GetType();
                var moduleAttribute = t.GetCustomAttribute<ModuleAttribute>(inherit: false);
                if (moduleAttribute != null)
                {
                    var section = builder.Configuration.GetSection(t.Name);
                    builder.Configuration.Bind(moduleAttribute.SectionName ?? t.Name, webModule);
                }

                ret.Add(webModule);
            }
        }

        return ret;
    }
}
