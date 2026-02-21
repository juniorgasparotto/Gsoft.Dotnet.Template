using Shared.UI.Api.Base;
using Shared.UI.Api.Base.Sdk;

await Startup
    .New(args)
    .AddSdk(SdkType.WorkerExecutor)
    .RunAsync();