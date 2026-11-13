namespace Kalei.Core.Ipc;

/// <summary>Direction of the frames a reader reads.</summary>
public enum Direction
{
    /// <summary>Worker to Core: what the Core reads and distrusts.</summary>
    ToCore,

    /// <summary>Core to Worker.</summary>
    ToWorker,
}

/// <summary>Which types travel in which direction, and the size cap of each (spec, "Size cap per type").</summary>
internal static class MessageTable
{
    internal readonly record struct Entry(Type Type, int Cap);

    private static readonly Dictionary<(string Name, Direction Direction), Entry> s_entries = new()
    {
        [("Hello", Direction.ToCore)] = new(typeof(HelloMessage), 4_096),
        [("Hello", Direction.ToWorker)] = new(typeof(HelloMessage), 64),
        [("Probe", Direction.ToWorker)] = new(typeof(ProbeMessage), 256),
        [("ProbeResult", Direction.ToCore)] = new(typeof(ProbeResultMessage), 512),
        [("Preview", Direction.ToWorker)] = new(typeof(PreviewMessage), 128),
        [("PreviewData", Direction.ToCore)] = new(typeof(PreviewDataMessage), 256),
        [("Convert", Direction.ToWorker)] = new(typeof(ConvertMessage), 32_768),
        [("Progress", Direction.ToCore)] = new(typeof(ProgressMessage), 128),
        [("Result", Direction.ToCore)] = new(typeof(ResultMessage), 256),
        [("Error", Direction.ToCore)] = new(typeof(ErrorMessage), 128),
        [("Cancel", Direction.ToWorker)] = new(typeof(CancelMessage), 64),
        [("Canary", Direction.ToWorker)] = new(typeof(CanaryMessage), 128),
        [("CanaryResult", Direction.ToCore)] = new(typeof(CanaryResultMessage), 256),
    };

    public static bool TryGet(string name, Direction direction, out Entry entry) => s_entries.TryGetValue((name, direction), out entry);

    /// <summary>The largest cap of a type over both directions (only Hello has two).</summary>
    public static int LargestCapOf(Type type) => s_entries.Values.Where(e => e.Type == type).Max(e => e.Cap);
}
