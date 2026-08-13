using WiuBroadcaster;
using WiuBroadcaster.Hubs;
using WiuBroadcaster.Models;
using WiuBroadcaster.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WiuOptions>(builder.Configuration.GetSection("Wiu"));
builder.Services.PostConfigure<WiuOptions>(o =>
{
    if (o.WiuIds.Length == 0) o.WiuIds = WiuOptions.DefaultWiuIds;
    if (o.Aspects.Length == 0) o.Aspects = WiuOptions.DefaultAspects;
});
builder.Services.AddSignalR();

builder.Services.AddSingleton<SubscriptionRegistry>();
builder.Services.AddSingleton<PendingChanges>();
builder.Services.AddSingleton<FlushLog>();

// BatchPublisher is resolved as a singleton first, then handed to the hosted-service
// pipeline. AddHostedService<BatchPublisher>() alone would create a second, separate
// instance, and POST /test/flush would then flush an object nobody is listening to.
builder.Services.AddSingleton<BatchPublisher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BatchPublisher>());
builder.Services.AddHostedService<WiuGenerator>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<WiuHub>("/hubs/wiu");

// ---------------------------------------------------------------------------
// Stage 5: make the registry visible.
// ---------------------------------------------------------------------------

app.MapGet("/debug", (
    SubscriptionRegistry registry,
    PendingChanges pending,
    FlushLog flushLog) =>
{
    var snapshot = registry.Snapshot();

    return Results.Json(new
    {
        snapshot.ActiveConnections,
        snapshot.UniqueGroups,
        PendingWiuChanges = pending.PendingCount,
        snapshot.Groups,
        snapshot.ReverseIndex,
        LastFlushNumber = flushLog.CurrentFlushNumber,
        RecentFlushes = flushLog.Recent(),
    });
});

app.MapGet("/debug/config", (Microsoft.Extensions.Options.IOptions<WiuOptions> options) =>
    Results.Json(options.Value));

// ---------------------------------------------------------------------------
// Test control surface. These exist so a driver can make the system fully
// deterministic: turn the random generator off, inject exactly the changes the
// scenario calls for, and flush on demand instead of waiting five seconds.
// ---------------------------------------------------------------------------

app.MapPost("/test/change", async (ChangeRequest request, PendingChanges pending) =>
{
    var recorded = new List<WiuUpdate>();

    foreach (var change in request.Changes)
    {
        var update = new WiuUpdate(
            change.WiuId,
            change.Aspect ?? "Clear",
            DateTimeOffset.UtcNow,
            pending.NextSequence());

        pending.Record(update);
        recorded.Add(update);
    }

    await Task.CompletedTask;
    return Results.Json(new { Recorded = recorded });
});

app.MapPost("/test/flush", async (BatchPublisher publisher) =>
    Results.Json(await publisher.FlushAsync()));

// The Vue client is a single-page app: anything that is not a real file and not one
// of the API routes above falls back to index.html.
app.MapFallbackToFile("index.html");

app.Run();

public record ChangeItem(string WiuId, string? Aspect);
public record ChangeRequest(ChangeItem[] Changes);
