using WiuBroadcaster.Models;

namespace WiuBroadcaster.Hubs;

/// <summary>
/// Strongly-typed client surface. These method names are the literal strings passed to
/// connection.on(...) in JS, so renaming one is a breaking wire change despite looking
/// like an ordinary C# rename.
/// </summary>
public interface IWiuClient
{
    /// <summary>A coalesced batch of changes, keyed by WIU id.</summary>
    Task ReceiveWiuBatch(Dictionary<string, WiuUpdate> batch);

    /// <summary>Full current state for the WIUs a client just subscribed to.</summary>
    Task ReceiveSnapshot(Dictionary<string, WiuUpdate> snapshot);

    /// <summary>Acknowledgement of a subscription change, for client-side display.</summary>
    Task SubscriptionChanged(string groupName, string[] wius);
}
