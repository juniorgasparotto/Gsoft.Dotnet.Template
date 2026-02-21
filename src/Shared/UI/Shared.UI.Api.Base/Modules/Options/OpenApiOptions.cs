using Shared.Infra.Module.Base.Attributes;

namespace Shared.UI.Api.Base.Modules.Options;

[ModuleOption("OpenApi")]
public class OpenApiOptions
{
    public string Title { get; set; }
    public string Description { get; set; }
}
