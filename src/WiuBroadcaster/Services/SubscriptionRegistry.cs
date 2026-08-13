using System.Collections.Concurrent;

namespace WiuBroadcaster.Services;

/// <summary>
/// The in-memory subscription registry: the only place that knows which SignalR
/// group name corresponds to which set of WIUs, and which groups care about a

/// </summary>
public class SubscriptionRegistry
{
    // group name -> the sorted WIU ids that define it
    private readonly ConcurrentDictionary<string, string[]> _groupToWius = new();

    // WIU id -> set of group names that include it (the reverse index)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _wiuToGroups = new();

    // group name -> set of connection ids currently in it
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _groupConnections = new();

    // connection id -> the single group it currently belongs to (a client has one layout at a time)
    private readonly ConcurrentDictionary<string, string> _connectionToGroup = new();

    /// <summary>
    /// Deduplicate, sort, and render the WIU list as a readable group name.
    /// </summary>
    public static string BuildGroupName(IEnumerable<string> wiuIds)
    {
        var normalized = NormalizeWius(wiuIds);
        return normalized.Length == 0
            ? "subscription:none"
            : "subscription:" + string.Join('|', normalized);
    }

    public static string[] NormalizeWius(IEnumerable<string> wiuIds) =>
        wiuIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// Point a connection at a new set of WIUs. Returns the group it left (if any)
    /// and the group it joined, so the hub knows which SignalR group calls to make.
    /// </summary>
    public (string? LeftGroup, string JoinedGroup, string[] Wius) Subscribe(
        string connectionId,
        IEnumerable<string> wiuIds)
    {
        var wius = NormalizeWius(wiuIds);
        var groupName = BuildGroupName(wius);

        var leftGroup = RemoveConnection(connectionId);
        if (leftGroup == groupName)
        {
            // Re-subscribing to the identical set: we still re-add below, so the
            // caller can safely issue AddToGroupAsync again (it is idempotent).
            leftGroup = null;
        }

        _groupToWius[groupName] = wius;
        _groupConnections.GetOrAdd(groupName, _ => new()).TryAdd(connectionId, 0);
        _connectionToGroup[connectionId] = groupName;

        foreach (var wiu in wius)
        {
            _wiuToGroups.GetOrAdd(wiu, _ => new()).TryAdd(groupName, 0);
        }

        return (leftGroup, groupName, wius);
    }

    /// <summary>
    /// Drop a connection from whatever group it was in. When the last connection
    /// leaves, the group and its reverse-index entries are torn down so the
    /// registry does not accumulate dead subscription groups.
    /// </summary>
    public string? RemoveConnection(string connectionId)
    {
        if (!_connectionToGroup.TryRemove(connectionId, out var groupName))
        {
            return null;
        }

        if (_groupConnections.TryGetValue(groupName, out var connections))
        {
            connections.TryRemove(connectionId, out _);

            if (connections.IsEmpty)
            {
                _groupConnections.TryRemove(groupName, out _);

                if (_groupToWius.TryRemove(groupName, out var wius))
                {
                    foreach (var wiu in wius)
                    {
                        if (_wiuToGroups.TryGetValue(wiu, out var groups))
                        {
                            groups.TryRemove(groupName, out _);
                            if (groups.IsEmpty)
                            {
                                _wiuToGroups.TryRemove(wiu, out _);
                            }
                        }
                    }
                }
            }
        }

        return groupName;
    }

    /// <summary>The reverse-index lookup the batch publisher lives on.</summary>
    public IReadOnlyCollection<string> GetGroupsForWiu(string wiuId) =>
        _wiuToGroups.TryGetValue(wiuId, out var groups)
            ? groups.Keys.ToArray()
            : Array.Empty<string>();

    public string[] GetWiusForGroup(string groupName) =>
        _groupToWius.TryGetValue(groupName, out var wius) ? wius : Array.Empty<string>();

    public string? GetGroupForConnection(string connectionId) =>
        _connectionToGroup.TryGetValue(connectionId, out var group) ? group : null;

    public int ConnectionCount => _connectionToGroup.Count;

    public int GroupCount => _groupToWius.Count;

    /// <summary>Flattened snapshot for the /debug endpoint and the driver's assertions.</summary>
    public RegistrySnapshot Snapshot() => new(
        ConnectionCount,
        GroupCount,
        _groupToWius
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => new GroupSnapshot(
                kvp.Key,
                kvp.Value,
                _groupConnections.TryGetValue(kvp.Key, out var c) ? c.Count : 0,
                _groupConnections.TryGetValue(kvp.Key, out var c2)
                    ? c2.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray()
                    : Array.Empty<string>()))
            .ToArray(),
        _wiuToGroups
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray()));
}

public record GroupSnapshot(
    string GroupName,
    string[] Wius,
    int ConnectionCount,
    string[] ConnectionIds);

public record RegistrySnapshot(
    int ActiveConnections,
    int UniqueGroups,
    GroupSnapshot[] Groups,
    Dictionary<string, string[]> ReverseIndex);
