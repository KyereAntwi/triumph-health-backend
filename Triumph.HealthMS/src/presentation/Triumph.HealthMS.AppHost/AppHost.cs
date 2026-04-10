var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("postgres-data")
    .AddDatabase("healthcare");

var redis = builder.AddRedis("redis");

var rabbit = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume("keycloak-data");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithHttpEndpoint(targetPort: 16686, name: "ui")
    .WithHttpEndpoint(targetPort: 4317, name: "otlp");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
    .WithBindMount("prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithHttpEndpoint(targetPort: 9090, name: "http");

builder.AddContainer("grafana", "grafana/grafana")
    .WithEnvironment(ctx =>
    {
        var prometheusEndpoint = prometheus.GetEndpoint("http");
        ctx.EnvironmentVariables["GF_DATASOURCES_PROMETHEUS_URL"] = prometheusEndpoint;
    })
    .WaitFor(prometheus);

var api = builder.AddProject<Projects.Triumph_HealthMS_Host>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbit)
    .WithReference(keycloak)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbit)
    .WaitFor(keycloak);

api.WithEnvironment(ctx =>
{
    ctx.EnvironmentVariables["RabbitMQ__Username"] = rabbit.Resource.UserNameParameter?.Value ?? "guest";
    ctx.EnvironmentVariables["RabbitMQ__Password"] = rabbit.Resource.PasswordParameter?.Value ?? "guest";
    ctx.EnvironmentVariables["RabbitMQ__Host"] = rabbit.Resource.PrimaryEndpoint;
    ctx.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] =  jaeger.GetEndpoint("otlp");
    ctx.EnvironmentVariables["AuthServer__Authority"] = keycloak.GetEndpoint("http");
});


builder.Build().Run();