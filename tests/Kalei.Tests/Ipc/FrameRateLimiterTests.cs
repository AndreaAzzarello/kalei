using Kalei.Core.Ipc;
using Xunit;

namespace Kalei.Tests.Ipc;

public sealed class FrameRateLimiterTests
{
    [Fact]
    public void OneHundredFramesInASecond_AreAllowed()
    {
        var limiter = new FrameRateLimiter(100, TimeSpan.FromSeconds(1));

        for (var i = 0; i < 100; i++)
        {
            Assert.True(limiter.Register(TimeSpan.FromMilliseconds(i * 9)));
        }
    }

    [Fact]
    public void TheHundredAndFirstFrameInTheSameSecond_IsRefused()
    {
        var limiter = new FrameRateLimiter(100, TimeSpan.FromSeconds(1));
        for (var i = 0; i < 100; i++)
        {
            Assert.True(limiter.Register(TimeSpan.FromMilliseconds(i * 9)));
        }

        Assert.False(limiter.Register(TimeSpan.FromMilliseconds(999)));
    }

    [Fact]
    public void ASustainedHonestRate_IsNeverRefused()
    {
        var limiter = new FrameRateLimiter(100, TimeSpan.FromSeconds(1));

        for (var i = 0; i < 1000; i++)
        {
            Assert.True(limiter.Register(TimeSpan.FromMilliseconds(i * 20))); // 50 per second for 20 s
        }
    }

    [Fact]
    public void AFrameJustOutsideTheWindow_IsAllowed()
    {
        var limiter = new FrameRateLimiter(100, TimeSpan.FromSeconds(1));
        for (var i = 0; i < 100; i++)
        {
            Assert.True(limiter.Register(TimeSpan.Zero + TimeSpan.FromMilliseconds(i)));
        }

        Assert.True(limiter.Register(TimeSpan.FromMilliseconds(1000)));
    }
}
