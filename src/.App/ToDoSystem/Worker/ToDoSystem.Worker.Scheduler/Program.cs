using Shared.UI.Api.Base;
using Shared.UI.Api.Base.Sdk;
using ToDoSystem.Worker.Scheduler;

await Startup
    .New(args)
    .AddSdk(SdkType.WorkerScheduler)
    .AddModule<SchedulerModule>()
    .RunAsync();
