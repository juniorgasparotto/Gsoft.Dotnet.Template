using Admin;
using Shared.UI.Api.Base;
using Shared.UI.Api.Base.Sdk;
using SqliteBrowser.UI;

await Startup
    .New(args)
    .AddSdk(SdkType.WorkerDashboard)
    .AddModule<SqliteBrowserUIModule>()
    .AddModule<AdminModule>()
    .RunAsync();
