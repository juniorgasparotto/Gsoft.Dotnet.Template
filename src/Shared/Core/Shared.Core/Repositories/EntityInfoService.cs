using Lombok.NET;

namespace Shared.Core.Repositories;

[Singleton]
public partial class EntityInfoService
{
    public class Info
    {
        public Type Type { get; set; } = null!;
        public Type TypeUserRef { get; set; } = null!;
        public string PropertyIdName { get; set; } = null!;
        public string PropertyTextName { get; set; } = null!;

        public string GetTypeName()
        {
            return Type.Name.ToLower();
        }
    }

    public class Info<T> : Info
    {
        public Func<RepositoryOptions, IQueryable<T>, IQueryable<T>> CompleteGlobalEntityQuery { get; set; } = null!;
    }

    private Dictionary<string, Info> TypeMapping => new Dictionary<string, Info>()
    {
        
    };

    public Info? GetEntityInfo(string? name)
    {
        var key = name?.ToLower();
        if (key != null && TypeMapping.ContainsKey(key))
            return TypeMapping[key];

        return null;
    }

    public Info? GetEntityInfo<T>()
    {
        return TypeMapping.Values.Where(f => f.Type == typeof(T)).FirstOrDefault();
    }
}