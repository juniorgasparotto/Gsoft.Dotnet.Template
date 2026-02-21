using Shared.Infra.Module.Hangfire.Workers;
using Shared.UI.Api.Base;
using Shared.UI.Api.Base.Sdk;

await Startup
    .New(args)
    .AddSdk(SdkType.WorkerJobs)
    .RunAsync();
