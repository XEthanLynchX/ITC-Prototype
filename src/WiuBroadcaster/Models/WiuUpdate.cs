namespace WiuBroadcaster.Models;

/// <summary>
/// One observed state change for a single Wayside Interface Unit.
/// This is the payload the batch publisher ships to clients.
/// </summary>
/// <param name="WiuId">Wayside Interface Unit identifier, e.g. "WIU-101".</param>
/// <param name="Aspect">Signal indication, e.g. "Clear" / "Approach" / "Stop".</param>
/// <param name="ObservedAtUtc">When the generator produced the change.</param>
/// <param name="Sequence">Monotonic counter, handy for spotting dropped updates in the client.</param>
public record WiuUpdate(
    string WiuId,
    string Aspect,
    DateTimeOffset ObservedAtUtc,
    long Sequence);
