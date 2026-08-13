using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using WiuBroadcaster.Hubs;
using WiuBroadcaster.Models;

namespace WiuBroadcaster.Services;

/// <summary>
/// Stage 4: the piece the whole practice app exists to demonstrate. Wakes on a
/// timer, drains the coalescing buffer, asks the registry which groups care about
/// each changed WIU, and publishes one filtered batch per group through
/// IHubContext — publishing to clients from outside the hub.
/// </summary>
public class BatchPublisher : BackgroundService
{
    private readonly IHubContext<WiuHub, IWiuClient> _hubContext;
    private readonly PendingChanges _pending;
    private readonly SubscriptionRegistry _registry;
    private readonly FlushLog _flushLog;
    private readonly WiuOptions _options;
    private readonly ILogger<BatchPublisher> _logger;

    public BatchPublisher(
        IHubContext<WiuHub, IWiuClient> hubContext,
        PendingChanges pending,
        SubscriptionRegistry registry,
        FlushLog flushLog,
        IOptions<WiuOptions> options,
        ILogger<BatchPublisher> logger)
    {
        _hubContext = hubContext;
        _pending = pending;
        _registry = registry;
        _flushLog = flushLog;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.FlushSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await FlushAsync();
            }
            catch (Exception ex)
            {
                // A throw here would kill the BackgroundService and silently stop
                // every future flush, so failures are logged and the loop continues.
                _logger.LogError(ex, "Flush failed");
            }
        }
    }

    /// <summary>
    /// One flush cycle. Public so POST /test/flush can force it on demand instead
    /// of a driver sleeping through a five-second window.
    /// </summary>
    public async Task<FlushRecord> FlushAsync()
    {
        var changes = _pending.TakeAll();
        var flushNumber = _flushLog.NextFlushNumber();

        var batchesByGroup = new Dictionary<string, Dictionary<string, WiuUpdate>>(StringComparer.Ordinal);

        foreach (var change in changes)
        {
            foreach (var group in _registry.GetGroupsForWiu(change.Key))
            {
                if (!batchesByGroup.TryGetValue(group, out var batch))
                {
                    batch = new Dictionary<string, WiuUpdate>(StringComparer.OrdinalIgnoreCase);
                    batchesByGroup[group] = batch;
                }

                batch[change.Key] = change.Value;
            }
        }

        foreach (var groupBatch in batchesByGroup)
        {
            await _hubContext.Clients.Group(groupBatch.Key).ReceiveWiuBatch(groupBatch.Value);
        }

        var record = new FlushRecord(
            flushNumber,
            DateTimeOffset.UtcNow,
            changes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            batchesByGroup
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => new FlushGroupDelivery(
                    kvp.Key,
                    kvp.Value.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray()))
                .ToArray());

        _flushLog.Add(record);

        if (changes.Count > 0)
        {
            _logger.LogInformation(
                "Flush {FlushNumber}: {ChangedCount} changed WIUs -> {GroupCount} groups",
                flushNumber,
                changes.Count,
                batchesByGroup.Count);
        }

        return record;
    }
}
