using Shared.Infra.Module.EntityFramework;
using Shared.UI.Api.Base;
using Shared.UI.Api.Base.Sdk;
using ToDoSystem.Infra;
using ToDoSystem.Infra.Repositories.EfCore;

await Startup
  .New(args)
  .AddSdk(SdkType.Web)
  .AddModule<EntityFrameworkModule, EntityFrameworkModuleBuilder>(
    new EntityFrameworkModuleBuilder()
      .AddDbContext<DefaultDbContext>()
  )
  .AddModule<ToDoSystemModule>()
  .RunAsync();
