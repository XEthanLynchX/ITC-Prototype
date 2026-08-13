using Microsoft.Extensions.Options;
using WiuBroadcaster.Models;

namespace WiuBroadcaster.Services;

/// <summary>
/// mutates one random WIU on a fast timer and drops the result into
/// PendingChanges. Deliberately never touches IHubContext — everything a client sees
/// goes through the publisher.
/// </summary>
public class WiuGenerator : BackgroundService
{
    private readonly PendingChanges _pending;
    private readonly WiuOptions _options;
    private readonly ILogger<WiuGenerator> _logger;
    private readonly Random _random;

    public WiuGenerator(
        PendingChanges pending,
        IOptions<WiuOptions> options,
        ILogger<WiuGenerator> logger)
    {
        _pending = pending;
        _options = options.Value;
        _logger = logger;
        _random = new Random(_options.GeneratorSeed);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.GeneratorEnabled)
        {
            _logger.LogInformation("WiuGenerator disabled (Wiu:GeneratorEnabled=false); only injected changes will flow");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.GeneratorIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var wiu = _options.WiuIds[_random.Next(_options.WiuIds.Length)];
            var aspect = _options.Aspects[_random.Next(_options.Aspects.Length)];
            var update = new WiuUpdate(wiu, aspect, DateTimeOffset.UtcNow, _pending.NextSequence());

            _pending.Record(update);
            _logger.LogInformation("Generated {WiuId} = {Aspect} (seq {Sequence})", wiu, aspect, update.Sequence);
        }
    }
}
