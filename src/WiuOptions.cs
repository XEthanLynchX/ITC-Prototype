namespace WiuBroadcaster;

/// <summary>
/// Bound from the "Wiu" config section. Override any value with a double-underscore
/// environment variable, e.g. WIU__GENERATORENABLED=false.
/// </summary>
public class WiuOptions
{
    public static readonly string[] DefaultWiuIds =
        ["WIU-101", "WIU-102", "WIU-103", "WIU-104", "WIU-105"];

    public static readonly string[] DefaultAspects =
        ["Clear", "Approach", "Restricting", "Stop"];

    /// <summary>
    /// WIUs the generator mutates and the UI offers. Empty on purpose — the config
    /// binder appends to arrays instead of replacing them, so an initializer here
    /// would concatenate with appsettings.json. PostConfigure in Program.cs fills
    /// the defaults in instead.
    /// </summary>
    public string[] WiuIds { get; set; } = [];

    /// <summary>Signal indications the generator picks from. Empty default, same reason as WiuIds.</summary>
    public string[] Aspects { get; set; } = [];

    /// <summary>
    /// When false the generator never runs, so the only changes in flight are those
    /// injected via POST /test/change. Drivers rely on this.
    /// </summary>
    public bool GeneratorEnabled { get; set; } = true;

    /// <summary>How often the generator mutates one random WIU.</summary>
    public int GeneratorIntervalMs { get; set; } = 500;

    /// <summary>How often the publisher drains pending changes and fans them out to groups.</summary>
    public int FlushSeconds { get; set; } = 10;

    /// <summary>RNG seed, so a generator-driven run is reproducible.</summary>
    public int GeneratorSeed { get; set; } = 1234;
}
