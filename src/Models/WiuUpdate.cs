namespace WiuBroadcaster.Models;

/// <summary>
/// One state change for a single Wayside Interface Unit — the payload the publisher
/// ships to clients.
/// </summary>
/// <param name="WiuId">Unit identifier, e.g. "WIU-101".</param>
/// <param name="Aspect">Signal indication, e.g. "Clear" / "Approach" / "Stop".</param>
/// <param name="ObservedAtUtc">When the generator produced the change.</param>
/// <param name="Sequence">Monotonic counter, for spotting dropped updates client-side.</param>
public record WiuUpdate(
    string WiuId,
    string Aspect,
    DateTimeOffset ObservedAtUtc,
    long Sequence);
