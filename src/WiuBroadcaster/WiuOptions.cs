namespace WiuBroadcaster;

/// <summary>
/// Bound from the "Wiu" configuration section. Every value can be overridden
/// with a double-underscore environment variable, e.g. WIU__GENERATORENABLED=false.
/// </summary>
public class WiuOptions
{
    public static readonly string[] DefaultWiuIds =
        ["WIU-101", "WIU-102", "WIU-103", "WIU-104", "WIU-105"];

    public static readonly string[] DefaultAspects =
        ["Clear", "Approach", "Restricting", "Stop"];

    /// <summary>
    /// Defaults are applied by PostConfigure in Program.cs otherwise they are formed from appsettings.json.
    /// </summary>
    public string[] WiuIds { get; set; } = [];

    /// <summary>Signal indications the generator picks from. Defaults are applied by PostConfigure in Program.cs.</summary>
    public string[] Aspects { get; set; } = [];

    /// <summary>
    /// When false the random generator never runs. Drivers turn this off so the
    /// only changes in flight are the ones they injected via POST /test/change.
    /// </summary>
    public bool GeneratorEnabled { get; set; } = true;

    /// <summary>How often the generator mutates one random WIU.</summary>
    public int GeneratorIntervalMs { get; set; } = 500;

    /// <summary>How often the publisher drains pending changes and fans them out to groups.</summary>
    public int FlushSeconds { get; set; } = 10;

    /// <summary>
    /// Seed for the generator's RNG so a generator-driven run is reproducible.
    /// </summary>
    public int GeneratorSeed { get; set; } = 1234;
}
