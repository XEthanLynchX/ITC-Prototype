using WiuBroadcaster.Models;

namespace WiuBroadcaster.Hubs;

/// <summary>
/// Strongly-typed client surface. The method names here are the literal strings a
/// JavaScript client passes to connection.on(...), so renaming one is a breaking
/// wire change even though it looks like an ordinary C# rename.
/// </summary>
public interface IWiuClient
{
    /// <summary>A coalesced batch of changes, keyed by WIU id.</summary>
    Task ReceiveWiuBatch(Dictionary<string, WiuUpdate> batch);

    /// <summary>Full current state for the WIUs a client just subscribed to.</summary>
    Task ReceiveSnapshot(Dictionary<string, WiuUpdate> snapshot);

    /// <summary>Server acknowledgement of a subscription change, for client-side display.</summary>
    Task SubscriptionChanged(string groupName, string[] wius);
}
