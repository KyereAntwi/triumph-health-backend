var builder = WebApplication.CreateBuilder(args);

var app = builder
    .AddServices()
    .AddPipelines();

app.Run();