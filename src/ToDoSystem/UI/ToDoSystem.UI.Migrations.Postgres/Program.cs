using Shared.Infra.Module.EntityFramework;
using Shared.UI.Api.Base;
using ToDoSystem.Infra.Repositories.EfCore;

await Startup
  .New(args)
  .AddModule<EntityFrameworkModule, EntityFrameworkModuleBuilder>(
    new EntityFrameworkModuleBuilder()
      .AddDbContext<DefaultDbContext>(ProviderType.Postgres))
  .RunAsync();
