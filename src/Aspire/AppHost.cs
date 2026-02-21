var builder = DistributedApplication.CreateBuilder(args);

//var apiService2 = builder.AddProject<Projects.BettingSystem_UI_Api>("BettingSystemApi")
//    .WithHttpHealthCheck("/health");

var apiService3 = builder.AddProject<Projects.ToDoSystem_UI_Api>("ToDoSystemApi")
    .WithHttpHealthCheck("/health");

var sqliteBrowserApi = builder.AddProject<Projects.SqliteBrowser_UI_Api>("SqliteBrowserApi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SqliteBrowser_UI_Web>("SqliteBrowserWeb")
    .WithReference(sqliteBrowserApi);

builder.AddProject<Projects.ToDoSystem_Worker_Jobs>("ToDoSystemWorkerJobs");
builder.AddProject<Projects.ToDoSystem_Worker_Executor>("ToDoSystemWorkerExecutor");
builder.AddProject<Projects.ToDoSystem_Worker_Scheduler>("ToDoSystemWorkerScheduler");
builder.AddProject<Projects.ToDoSystem_Worker_Dashboard>("ToDoSystemWorkerDashboard");

builder.Build().Run();
