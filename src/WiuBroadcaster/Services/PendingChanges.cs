using System.Collections.Concurrent;
using WiuBroadcaster.Models;

namespace WiuBroadcaster.Services;

/// <summary>
/// The coalescing buffer between the generator and the publisher. Keyed by WIU id,
/// so a WIU that changes ten times inside one flush window still ships exactly one
/// update — its latest state. That collapse is the whole point of the buffer.
/// </summary>
public class PendingChanges
{
    private readonly ConcurrentDictionary<string, WiuUpdate> _pending = new();

    // Latest known state of every WIU, kept forever so a newly subscribed client
    // can be handed a snapshot instead of waiting up to a full flush window.
    private readonly ConcurrentDictionary<string, WiuUpdate> _lastKnown = new();

    private long _sequence;

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    public void Record(WiuUpdate update)
    {
        _pending[update.WiuId] = update;
        _lastKnown[update.WiuId] = update;
    }

    public int PendingCount => _pending.Count;

    /// <summary>
    /// Atomically drain the buffer. Uses TryRemove per key rather than Clear() so a
    /// change written by the generator mid-drain is either taken by this flush or
    /// left for the next one — never silently dropped, which Clear() would allow.
    /// </summary>
    public Dictionary<string, WiuUpdate> TakeAll()
    {
        var taken = new Dictionary<string, WiuUpdate>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in _pending.Keys.ToArray())
        {
            if (_pending.TryRemove(key, out var update))
            {
                taken[key] = update;
            }
        }

        return taken;
    }

    /// <summary>Current state of the requested WIUs, for the on-subscribe snapshot.</summary>
    public Dictionary<string, WiuUpdate> SnapshotFor(IEnumerable<string> wiuIds)
    {
        var result = new Dictionary<string, WiuUpdate>(StringComparer.OrdinalIgnoreCase);

        foreach (var wiu in wiuIds)
        {
            if (_lastKnown.TryGetValue(wiu, out var update))
            {
                result[wiu] = update;
            }
        }

        return result;
    }
}
