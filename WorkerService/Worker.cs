using Medallion.Threading.Redis;
using Serilog.Context;
using StackExchange.Redis;

namespace WorkerService;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;
  private readonly IConnectionMultiplexer _connectionMultiplexer;

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
      await Work(stoppingToken);
    }
  }

  private async Task Work(CancellationToken cancellationToken)
  {
    using var scope = LogContext.PushProperty("OperationId", Guid.NewGuid());
    var @lock = new RedisDistributedLock("Lock", _connectionMultiplexer.GetDatabase());
    await using var handle = await @lock.TryAcquireAsync(TimeSpan.FromSeconds(3), cancellationToken);
    if (handle is not null)
    {
      try
      {
        _logger.LogInformation("Acquired lock");
        await using var fileStream = new FileStream("locking.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        await Task.Delay(TimeSpan.FromMilliseconds(3700), cancellationToken);
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Hit distributed lock error");
      }
    }
    else
    {
      _logger.LogInformation("Did not acquire lock");
    }
  }
}