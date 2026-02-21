using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;

namespace Shared.Infra.Module.Base
{
    public class WebModuleConfigurationOptions(WebApplicationBuilder webApplicationBuilder)
    {
        private readonly int envIndex = webApplicationBuilder.Configuration.Sources
            .Select((s, i) => (s, i))
            .FirstOrDefault(x => x.s is EnvironmentVariablesConfigurationSource).i;

        private readonly bool hasPlaceHolder = webApplicationBuilder.Configuration.Sources
            .Select((s, i) => (s, i))
            .FirstOrDefault(x => x.s.GetType().Name == "PlaceholderConfigurationSource").s != null;
        
        public void AddJsonFile(string path, bool optional, bool reloadOnChange)
        {
            if (hasPlaceHolder)
            {
                var a = webApplicationBuilder.Configuration.Sources[0];
            }

            if (envIndex > 0)
            {
                webApplicationBuilder.Configuration.Sources.Insert(envIndex, new JsonConfigurationSource
                {
                    Path = path,
                    Optional = optional,
                    ReloadOnChange = reloadOnChange
                });
            }
            else
            {
                webApplicationBuilder.Configuration.AddJsonFile(path, optional, reloadOnChange);
            }
        }

        public void AddIniFile(string path, bool optional, bool reloadOnChange)
        {
            if (envIndex > 0)
            {
                webApplicationBuilder.Configuration.Sources.Insert(envIndex, new IniConfigurationSource
                {
                    Path = path,
                    Optional = optional,
                    ReloadOnChange = reloadOnChange
                });
            }
            else
            {
                webApplicationBuilder.Configuration.AddIniFile(path, optional, reloadOnChange);
            }
        }

        public void AddInMemoryCollection(IEnumerable<KeyValuePair<string, string?>>? initialData)
        {
            if (envIndex > 0)
            {
                webApplicationBuilder.Configuration.Sources.Insert(envIndex, new MemoryConfigurationSource
                {
                    InitialData = initialData
                });
            }
            else
            {
                webApplicationBuilder.Configuration.AddInMemoryCollection(initialData);
            }
        }
    }

    public interface IWebModule
    {
        void AddConfigurations(WebModuleConfigurationOptions options) { }
        void ConfigureHost(WebApplicationBuilder builder);
        void Configure(WebApplication app);
    }
}
