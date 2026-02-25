using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Shared.Infra.Module.Base.Utils;

/// <summary>
/// Helper que usa o método Merge() nativo do Newtonsoft.Json para fazer merge de arquivos JSON
/// e retorna um Dictionary para usar com AddInMemoryCollection (sem escrever em disco).
/// </summary>
public static class JsonConfigurationMerger
{
    /// <summary>
    /// Faz merge de múltiplos arquivos JSON usando o método Merge() nativo do JObject
    /// e retorna um Dictionary para usar com AddInMemoryCollection.
    /// </summary>
    /// <param name="jsonFiles">Caminhos dos arquivos JSON para fazer merge (na ordem de prioridade)</param>
    /// <returns>Dictionary com as configurações mescladas no formato chave:valor</returns>
    public static Dictionary<string, string?> MergeJsonFilesToDictionary(params string[] jsonFiles)
    {
        if (jsonFiles.Length == 0)
            throw new ArgumentException("At least one JSON file must be provided", nameof(jsonFiles));

        // Filtrar apenas arquivos que existem
        var existingFiles = jsonFiles.Where(File.Exists).ToArray();
        
        if (existingFiles.Length == 0)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        JObject mergedJson;

        if (existingFiles.Length == 1)
        {
            mergedJson = JObject.Parse(File.ReadAllText(existingFiles[0]));
        }
        else
        {
            // Ler o primeiro arquivo como base
            mergedJson = JObject.Parse(File.ReadAllText(existingFiles[0]));

            // Fazer merge com cada arquivo subsequente usando o método nativo Merge()
            for (int i = 1; i < existingFiles.Length; i++)
            {
                var overrideJson = JObject.Parse(File.ReadAllText(existingFiles[i]));
                // Merge nativo do Newtonsoft.Json - combina arrays e mescla objetos recursivamente
                mergedJson.Merge(overrideJson, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union, // Combina arrays (adiciona ao final)
                    MergeNullValueHandling = MergeNullValueHandling.Ignore // Ignora valores null
                });
            }
        }

        // Converter JObject para Dictionary<string, string?> (formato IConfiguration)
        return FlattenJObject(mergedJson);
    }

    /// <summary>
    /// Converte um JObject para Dictionary<string, string?> no formato usado pelo IConfiguration.
    /// </summary>
    private static Dictionary<string, string?> FlattenJObject(JObject obj, string prefix = "")
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in obj.Properties())
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
            FlattenToken(property.Value, key, dict);
        }

        return dict;
    }

    private static void FlattenToken(JToken token, string key, Dictionary<string, string?> dict)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in ((JObject)token).Properties())
                {
                    FlattenToken(property.Value, $"{key}:{property.Name}", dict);
                }
                break;

            case JTokenType.Array:
                var index = 0;
                foreach (var item in ((JArray)token))
                {
                    FlattenToken(item, $"{key}:{index}", dict);
                    index++;
                }
                break;

            case JTokenType.String:
                dict[key] = token.Value<string>();
                break;

            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.Boolean:
                dict[key] = token.ToString();
                break;

            case JTokenType.Null:
                dict[key] = null;
                break;

            default:
                dict[key] = token.ToString();
                break;
        }
    }
}
