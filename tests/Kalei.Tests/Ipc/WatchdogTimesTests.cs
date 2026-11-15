using Kalei.Core.Ipc;
using Xunit;

namespace Kalei.Tests.Ipc;

public sealed class WatchdogTimesTests
{
    [Fact]
    public void Default_HelloTimeout_IsFifteenSeconds_ToHoldTheSlowestColdStartMeasuredOnTheVm()
    {
        // 5.39 s was measured on the Windows 10 VM after a reboot (ticket 07b); the 5 s of the first draft would have failed it.
        Assert.Equal(TimeSpan.FromSeconds(15), WatchdogTimes.Default.Hello);
    }

    [Fact]
    public void Default_OtherTimers_AreUnchanged()
    {
        Assert.Equal(TimeSpan.FromSeconds(10), WatchdogTimes.Default.Heartbeat);
        Assert.Equal(TimeSpan.FromSeconds(3), WatchdogTimes.Default.CancelGrace);
        Assert.Equal(TimeSpan.FromSeconds(3), WatchdogTimes.Default.FinalGrace);
    }
}
