using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkerContagem;
using WorkerContagem.Data;
using WorkerContagem.Kafka;
using WorkerContagem.Tracing;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        if (KafkaExtensions.CreateTopicForTestsIfNotExists(hostContext.Configuration))
            Console.WriteLine(
                $"Criado o t�pico {hostContext.Configuration["ApacheKafka:Topic"]} com 10 parti��es para testes...");

        services.AddSingleton<ContagemRepository>();
        services.AddOpenTelemetryTracing(traceProvider =>
        {
            traceProvider
                .AddSource(OpenTelemetryExtensions.ServiceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                            serviceVersion: OpenTelemetryExtensions.ServiceVersion))
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation(
                    options => options.SetDbStatementForText = true)
                .AddJaegerExporter(exporter =>
                {
                    exporter.AgentHost = hostContext.Configuration["Jaeger:AgentHost"];
                    exporter.AgentPort = Convert.ToInt32(hostContext.Configuration["Jaeger:AgentPort"]);
                });
        });

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();