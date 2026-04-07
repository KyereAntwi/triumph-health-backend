var builder = DistributedApplication.CreateBuilder(args);

//var db = builder.AddPostgres("db");

builder.AddProject<Projects.Triumph_HealthMS_Host>("api");

builder.Build().Run();