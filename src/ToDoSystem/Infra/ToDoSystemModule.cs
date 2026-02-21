using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;

namespace ToDoSystem.Infra;

/// <summary>
/// Module to register repositories and DbContext for ToDoSystem.
/// </summary>
[Module(
    Title = "ToDoSystem",
    Description = "Module Principal do Sistema ToDoList"
)]
public class ToDoSystemModule : IWebModule
{
    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {
    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {

    }

    public void Configure(WebApplication app)
    {
    }
}
