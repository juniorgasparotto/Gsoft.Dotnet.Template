using Shared.Infra.Module.Base.Attributes;

namespace Shared.UI.Api.Base.Modules.Options;

[ModuleOption("OpenApi")]
public class OpenApiOptions
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
