using Microsoft.Extensions.Options;
using WorkerService;
using Serilog;
using StackExchange.Redis;

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.UseSerilog((context, serviceProvider, configuration) =>
{
  configuration.Enrich.FromLogContext()
    .Enrich.WithProcessId()
    .Enrich.WithProcessName()
    .Enrich.WithThreadId()
    .Enrich.WithThreadName()
    .Enrich.WithProperty("ContainerId", Environment.GetEnvironmentVariable("HOSTNAME"))
    .WriteTo.Seq(context.Configuration.GetValue<string>("Seq"));
});
hostBuilder.ConfigureServices((context, services) =>
{
  services.AddOptions();
  services.Configure<ObjectStoreSettings>(objectStoreSettings => context.Configuration.GetSection("ObjectStore").Bind(objectStoreSettings));
  services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
  {
    var nodes = serviceProvider.GetRequiredService<IOptionsMonitor<ObjectStoreSettings>>().CurrentValue.Nodes;
    return ConnectionMultiplexer.Connect(string.Join(',', nodes));
  });
  services.AddHostedService<Worker>();
});

Log.Logger = new LoggerConfiguration()
  .Enrich.FromLogContext()
  .WriteTo.Console()
  .CreateBootstrapLogger();

try
{
  var host = hostBuilder.Build();
  await host.RunAsync();
}
catch (Exception ex)
{
  Log.Error(ex, "Unhandled exception");
}
finally
{
  await Log.CloseAndFlushAsync();
}