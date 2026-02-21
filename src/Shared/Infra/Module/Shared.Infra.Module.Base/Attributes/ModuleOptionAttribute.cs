namespace Shared.Infra.Module.Base.Attributes;

/// <summary>
/// Atributo para especificar o nome da seção de configuração para uma classe de opções.
/// Se não especificado, será usado o comportamento padrão.
/// </summary>
/// <remarks>
/// Inicializa uma nova instância do atributo com o nome da seção.
/// </remarks>
/// <param name="sectionName">Nome da seção de configuração.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModuleOptionAttribute(string? sectionName = null) : Attribute
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public string Name { get; } = sectionName;
}