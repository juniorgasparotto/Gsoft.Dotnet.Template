var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.ToDoSystem_UI_Api>("todystem-ui-api");
var executor = builder.AddProject<Projects.ToDoSystem_Worker_Executor>("todystem-worker-executor");
var scheduler = builder.AddProject<Projects.ToDoSystem_Worker_Scheduler>("todystem-worker-scheduler");
builder.AddProject<Projects.ToDoSystem_Worker_Dashboard>("todystem-worker-dashboard");
builder.AddProject<Projects.ToDoSystem_Worker_Jobs>("todystem-worker-jobs");
builder.AddProject<Projects.Admin>("admin");

builder.Build().Run();
