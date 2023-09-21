using Medallion.Threading.Redis;
using Serilog.Context;
using StackExchange.Redis;

namespace WorkerService;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;
  private readonly IConnectionMultiplexer _connectionMultiplexer;

  private const string Key = "NumberKey";

  public Worker(
    ILogger<Worker> logger,
    IConnectionMultiplexer connectionMultiplexer)
  {
    _logger = logger;
    _connectionMultiplexer = connectionMultiplexer;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      await Parallel.ForEachAsync(
        Enumerable.Range(1, Environment.ProcessorCount),
        stoppingToken,
        async (x, innerToken) =>
        {
          using var scope = LogContext.PushProperty("OperationId", Guid.NewGuid());
          try
          {
            var @lock = new RedisDistributedLock($"Lock#{x}", _connectionMultiplexer.GetDatabase());
            await using var handle = await @lock.TryAcquireAsync(TimeSpan.FromSeconds(1), innerToken);
            if (handle != null)
            {
              _logger.LogInformation("Acquired lock {Lock}", x);
              await Task.Delay(TimeSpan.FromMilliseconds(1200), innerToken);
              _logger.LogInformation("Released lock {Lock}", x);
            }
            else
            {
              _logger.LogInformation("Did not acquire lock {Lock}", x);
            }
          }
          catch (Exception e)
          {
            _logger.LogWarning(e, "Error occurred acquiring lock {Lock}", x);
          }
        });
    }
  }
}