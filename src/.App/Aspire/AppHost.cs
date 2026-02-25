var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ToDoSystem_UI_Api>("ToDoSystemApi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ToDoSystem_Worker_Jobs>("ToDoSystemWorkerJobs");
builder.AddProject<Projects.ToDoSystem_Worker_Executor>("ToDoSystemWorkerExecutor");
builder.AddProject<Projects.ToDoSystem_Worker_Scheduler>("ToDoSystemWorkerScheduler");
builder.AddProject<Projects.ToDoSystem_Worker_Dashboard>("ToDoSystemWorkerDashboard");

builder.Build().Run();
