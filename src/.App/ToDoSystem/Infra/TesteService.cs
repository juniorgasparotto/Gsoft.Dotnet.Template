using Lombok.NET;
using Shared.Core.Attributes;

namespace ToDoSystem.Infra;

public interface ITesteService
{

}

public interface ITesteService2
{

}

[Singleton]
[ToString]
[InjectAsScoped]
public partial class TesteService
{
        public void Teste()
        {
            Console.WriteLine("Teste");
        }
}
