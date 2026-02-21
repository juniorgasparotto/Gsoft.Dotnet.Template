namespace Shared.UI.Api.Base.Bootstrap;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Shared.Infra.Module.Base;

/// <summary>
/// Interface para resolver e construir configurações dos módulos.
/// </summary>
public interface IConfigurationResolver
{
    /// <summary>
    /// Cria um IConfiguration bootstrap chamando os métodos estáticos AddConfigurations de todos os módulos.
    /// </summary>
    void CreateBootstrapConfiguration(
        IHostApplicationBuilder builder,
        IEnumerable<object> modules,
        string[] args
    );

    /// <summary>
    /// Configura o IConfiguration da aplicação final, chamando AddConfigurations de instância dos módulos.
    /// </summary>
    void ConfigureApplicationConfiguration(
        WebApplicationBuilder builder,
        IEnumerable<IWebModule> modules,
        string[] args
    );
}
