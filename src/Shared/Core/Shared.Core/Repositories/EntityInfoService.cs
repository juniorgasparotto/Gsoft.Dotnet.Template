using Lombok.NET;

namespace Shared.Core.Repositories;

[Singleton]
public partial class EntityInfoService
{
    public class Info
    {
        public Type Type { get; set; }
        public Type TypeUserRef { get; set; }
        public string PropertyIdName { get; set; }
        public string PropertyTextName { get; set; }

        public string GetTypeName()
        {
            return Type.Name.ToLower();
        }
    }

    public class Info<T> : Info
    {
        public Func<RepositoryOptions, IQueryable<T>, IQueryable<T>> CompleteGlobalEntityQuery { get; set; }
    }

    private Dictionary<string, Info> TypeMapping => new Dictionary<string, Info>()
    {
        
    };

    public Info GetEntityInfo(string name)
    {
        name = name?.ToLower();
        if (name != null && TypeMapping.ContainsKey(name))
            return TypeMapping[name];

        return null;
    }

    public Info GetEntityInfo<T>()
    {
        return TypeMapping.Values.Where(f => f.Type == typeof(T)).FirstOrDefault();
    }
}