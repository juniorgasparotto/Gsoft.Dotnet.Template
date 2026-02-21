namespace Shared.Infra.Module.Base.Attributes;

/// <summary>
/// Atributo para especificar configurações de módulos
/// <paramref name="configPath"/>
/// <paramref name="name"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModuleAttribute() : Attribute
{
    /// <summary>
    /// Título do módulo.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Descrição do módulo.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Caminho do arquivo de configuração.
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public string? SectionName { get; set; }
}
