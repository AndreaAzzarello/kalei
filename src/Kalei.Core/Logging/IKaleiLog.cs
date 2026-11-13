namespace Kalei.Core.Logging;

/// <summary>
/// The only way Kalei writes a log. Its single input is a <see cref="LogEvent"/>, which has no text field;
/// the file writer with rotation is ticket 26.
/// </summary>
public interface IKaleiLog
{
    void Write(LogEvent logEvent);
}
