namespace Shared.Infra.Module.EntityFramework.Options;

/// <summary>
/// Configuração de uma convenção por DbContext.
/// </summary>
public class ConventionConfiguration
{
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Para EnumAsTableModelCustomizer: se deve usar HasData (seed dos valores do enum).
    /// </summary>
    public bool HasData { get; set; } = true;

    /// <summary>
    /// Para EnumAsTableModelCustomizer: enums a incluir. ["*"] = todos.
    /// </summary>
    public List<string> EnumInclude { get; set; } = [];

    /// <summary>
    /// Para EnumAsTableModelCustomizer: enums a excluir.
    /// </summary>
    public List<string> EnumExclude { get; set; } = [];
}
