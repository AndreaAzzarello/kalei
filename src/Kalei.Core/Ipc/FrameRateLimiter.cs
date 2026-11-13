namespace Kalei.Core.Ipc;

/// <summary>More than max frames inside any window ends the Worker (spec, "Rate cap").</summary>
public sealed class FrameRateLimiter(int max, TimeSpan window)
{
    private readonly TimeSpan[] _times = new TimeSpan[max];
    private int _next;
    private int _count;

    /// <summary>Registers a frame received at <paramref name="now"/>; false if it is one too many.</summary>
    public bool Register(TimeSpan now)
    {
        if (_count == max && now - _times[_next] < window)
        {
            return false;
        }

        _times[_next] = now;
        _next = (_next + 1) % max;
        _count = Math.Min(_count + 1, max);
        return true;
    }
}
