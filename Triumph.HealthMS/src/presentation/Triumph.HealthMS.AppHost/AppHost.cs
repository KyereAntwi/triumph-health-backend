var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddConnectionString("healthcare");
var redis = builder.AddConnectionString("redis");
var rabbit = builder.AddConnectionString("messaging");

var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume("keycloak-data");

var api = builder.AddDockerfile("api", "../../../", "src/presentation/Triumph.HealthMS.Host/Dockerfile")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithHttpsEndpoint(targetPort: 8081, name: "https")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbit)
    .WithReference(keycloak)
    .WaitFor(keycloak);

api.WithEnvironment(ctx =>
{
    ctx.EnvironmentVariables["AuthServer__Authority"] = keycloak.GetEndpoint("https");
    ctx.EnvironmentVariables["AuthServer__Realm"] = "realms/triumph-healthsm";
    ctx.EnvironmentVariables["AuthServer__Audience"] = "health-ms";
});


builder.Build().Run();