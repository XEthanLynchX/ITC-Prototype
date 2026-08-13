using System.Collections.Concurrent;

namespace WiuBroadcaster.Services;

/// <summary>
/// Ring buffer of recent flushes. The console log scrolls away; this is what
/// /debug and the driver read to prove a specific flush routed a specific WIU
/// to a specific group.
/// </summary>
public class FlushLog
{
    private const int Capacity = 25;

    private readonly ConcurrentQueue<FlushRecord> _records = new();

    private long _flushNumber;

    public long CurrentFlushNumber => Interlocked.Read(ref _flushNumber);

    public long NextFlushNumber() => Interlocked.Increment(ref _flushNumber);

    public void Add(FlushRecord record)
    {
        _records.Enqueue(record);

        while (_records.Count > Capacity && _records.TryDequeue(out _))
        {
            // trim
        }
    }

    /// <summary>Most recent flush first — the order a human debugging wants.</summary>
    public FlushRecord[] Recent() => _records.Reverse().ToArray();
}

public record FlushRecord(
    long FlushNumber,
    DateTimeOffset AtUtc,
    string[] ChangedWius,
    FlushGroupDelivery[] Deliveries);

public record FlushGroupDelivery(string GroupName, string[] SentWius);
