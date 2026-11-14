using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kalei.Core.Ipc;

/// <summary>Ranges, patterns and direction rules of every field (spec, "Messages"). Everything that needs session state is in the session.</summary>
internal static partial class MessageValidator
{
    private const long MaxExactInteger = 9_007_199_254_740_991;

    [GeneratedRegex("^[A-Za-z0-9._+-]{1,64}$")]
    private static partial Regex WorkerId();

    [GeneratedRegex("^[a-z0-9-]{1,32}$")]
    private static partial Regex ModuleId();

    [GeneratedRegex(@"^\d+\.\d+\.\d+([-+][0-9A-Za-z.+-]*)?$")]
    private static partial Regex Semver();

    [GeneratedRegex("^[a-z0-9]{1,16}$")]
    private static partial Regex FormatId();

    [GeneratedRegex("^[a-z0-9-]{1,32}$")]
    private static partial Regex Encoding();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]{0,31}$")]
    private static partial Regex OptionKey();

    public static void Validate(IpcMessage message, Direction direction)
    {
        switch (message)
        {
            case HelloMessage m:
                Hello(m, direction);
                break;
            case ProbeMessage m:
                Check(m.Source >= 1 && (m.Candidate is null || FormatId().IsMatch(m.Candidate)));
                break;
            case ProbeResultMessage m:
                ProbeResult(m);
                break;
            case PreviewMessage m:
                Check(m.Source >= 1 && m.MaxSize is >= IpcLimits.MinPreviewSize and <= IpcLimits.MaxPreviewSize);
                break;
            case PreviewDataMessage m:
                Check(m.Width is >= 1 and <= IpcLimits.MaxPreviewSize && m.Height is >= 1 and <= IpcLimits.MaxPreviewSize
                    && (long)m.Width * m.Height * 4 == m.Length);
                break;
            case ConvertMessage m:
                Convert(m);
                break;
            case ProgressMessage m:
                Check(!(m.Fraction is not null && m.Bytes is not null)
                    && (m.Fraction is null || (double.IsFinite(m.Fraction.Value) && m.Fraction is >= 0 and <= 1))
                    && (m.Bytes is null || m.Bytes is >= 0 and <= MaxExactInteger));
                break;
            case ResultMessage m:
                Check(m.OutputBytes is >= 0 and <= MaxExactInteger && (m.Warnings is null || m.Warnings.Distinct().Count() == m.Warnings.Count));
                break;
            case ErrorMessage m:
                Check(m.Code.IsWorkerSendable());
                break;
            case CanaryMessage m:
                Check(m.LocalPort is >= 1024 and <= 65535);
                break;
            case CanaryResultMessage m:
                Check(CanaryProbeIsValid(m.Local) && CanaryProbeIsValid(m.Net));
                break;
        }
    }

    private static void Hello(HelloMessage m, Direction direction)
    {
        if (direction == Direction.ToWorker)
        {
            // The Core's reply carries only the version.
            if (m.Worker is not null || m.Modules is not null)
            {
                throw new IpcException(ProtocolReason.UnknownField);
            }

            return;
        }

        if (m.Worker is null || m.Modules is null)
        {
            throw new IpcException(ProtocolReason.MissingField);
        }

        Check(WorkerId().IsMatch(m.Worker) && m.Modules.Count is >= 1 and <= IpcLimits.MaxModules
            && m.Modules.All(x => ModuleId().IsMatch(x.Id) && x.Version.Length <= 32 && Semver().IsMatch(x.Version)));
    }

    private static void ProbeResult(ProbeResultMessage m)
    {
        var p = m.Properties;
        Check(FormatId().IsMatch(m.Format)
            && (p.Width is null || p.Width is >= 1 and <= 1_000_000)
            && (p.Height is null || p.Height is >= 1 and <= 1_000_000)
            && (p.Frames is null || p.Frames is >= 1 and <= 100_000)
            && (p.Sheets is null || p.Sheets is >= 1 and <= 10_000)
            && (p.Encoding is null || Encoding().IsMatch(p.Encoding)));
    }

    private static void Convert(ConvertMessage m)
    {
        var l = m.Limits;
        Check(FormatId().IsMatch(m.From) && FormatId().IsMatch(m.To) && m.Source >= 1 && m.Output >= 1
            && m.Options.Count <= IpcLimits.MaxOptions
            && l.Pixels is >= 1 and <= 500_000_000 && l.Depth is >= 1 and <= 64 && l.YamlAliases is >= 0 and <= 100
            && l.ExpansionRatio is >= 1 and <= 100 && l.UncompressedBytes is >= 1 and <= 1_073_741_824);

        foreach (var (key, value) in m.Options)
        {
            Check(OptionKey().IsMatch(key) && OptionValueIsValid(value));
        }
    }

    private static bool OptionValueIsValid(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True or JsonValueKind.False => true,
        JsonValueKind.Number => value.TryGetInt64(out var n) && Math.Abs(n) <= MaxExactInteger,
        JsonValueKind.String => System.Text.Encoding.UTF8.GetByteCount(value.GetString()!) <= IpcLimits.MaxOptionStringBytes,
        _ => false,
    };

    private static bool CanaryProbeIsValid(CanaryProbe probe) => (probe.Outcome == CanaryOutcome.Error) == (probe.Code is not null);

    private static void Check(bool valid)
    {
        if (!valid)
        {
            throw new IpcException(ProtocolReason.BadValue);
        }
    }
}
