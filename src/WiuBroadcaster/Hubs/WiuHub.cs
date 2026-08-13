using Microsoft.AspNetCore.SignalR;
using WiuBroadcaster.Services;

namespace WiuBroadcaster.Hubs;

public class WiuHub : Hub<IWiuClient>
{
    private readonly SubscriptionRegistry _registry;
    private readonly PendingChanges _pending;
    private readonly ILogger<WiuHub> _logger;

    public WiuHub(SubscriptionRegistry registry, PendingChanges pending, ILogger<WiuHub> logger)
    {
        _registry = registry;
        _pending = pending;
        _logger = logger;
    }

    /// <summary>
    /// The only entry point. The client sends the WIUs it wants; the server derives
    /// the group name, moves the connection, and immediately answers with a snapshot
    /// so the client is not blank until the next five-second flush.
    /// </summary>
    public async Task Subscribe(string[] wiuIds)
    {
        var (leftGroup, joinedGroup, wius) = _registry.Subscribe(Context.ConnectionId, wiuIds);

        if (leftGroup is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, leftGroup);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, joinedGroup);

        _logger.LogInformation(
            "Subscribe {ConnectionId}: {LeftGroup} -> {JoinedGroup}",
            Context.ConnectionId,
            leftGroup ?? "(none)",
            joinedGroup);

        await Clients.Caller.SubscriptionChanged(joinedGroup, wius);
        await Clients.Caller.ReceiveSnapshot(_pending.SnapshotFor(wius));
    }

    /// <summary>
    /// Leave the current subscription group without disconnecting — what a browser
    /// tab does when it is hidden.
    /// </summary>
    public async Task Unsubscribe()
    {
        var leftGroup = _registry.RemoveConnection(Context.ConnectionId);

        if (leftGroup is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, leftGroup);
            _logger.LogInformation("Unsubscribe {ConnectionId} from {Group}", Context.ConnectionId, leftGroup);
        }

        await Clients.Caller.SubscriptionChanged("(none)", Array.Empty<string>());
    }

    /// <summary>Lets a client label itself in /debug output. Purely a debugging aid.</summary>
    public Task<string> WhoAmI() => Task.FromResult(Context.ConnectionId);

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Connected {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var leftGroup = _registry.RemoveConnection(Context.ConnectionId);
        _logger.LogInformation(
            "Disconnected {ConnectionId} (was in {Group})",
            Context.ConnectionId,
            leftGroup ?? "(none)");
        return base.OnDisconnectedAsync(exception);
    }
}
