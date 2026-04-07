var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// aspire configurations
builder.AddServiceDefaults();
builder.AddRedisClient(connectionName: "redis");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

app.Run();