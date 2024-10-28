using LowBandwidthDtFunction.MqttBridge;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        ILogger<MqttBridge> mqttLogger;

        using (ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
        {
            mqttLogger = loggerFactory.CreateLogger<MqttBridge>();
        }

        // Registering an instance ensures that the MqttBridge is invoked and immediately running.
        services.AddSingleton(new MqttBridge(mqttLogger));
    })
    .Build();

host.Run();
