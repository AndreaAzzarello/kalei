using System.Runtime.Versioning;
using Kalei.Core.Ipc;
using Kalei.Tests.Ipc;
using Xunit;

namespace Kalei.Tests.Platform;

/// <summary>The real Worker, started by the real Windows launcher: AppContainer, Job Object, inherited anonymous pipes.</summary>
[SupportedOSPlatform("windows")]
public sealed class SandboxLauncherTests(SandboxFixture sandbox) : IClassFixture<SandboxFixture>
{
    [Fact]
    public async Task TheRealWorker_InTheSandbox_SendsItsHello_AndAnswersEFormat()
    {
        var launcher = sandbox.Launcher(BuiltinWorkerTests.BuiltinExe);
        var log = new BuiltinWorkerTests.NoLog();
        var session = new WorkerSession(launcher, new BuiltinWorkerTests.BuiltinCatalog(), log, WatchdogTimes.Default);

        var outcome = await session.RunAsync(new ProbeMessage { Source = 4 }, new RequestOptions { Timeout = TimeSpan.FromSeconds(30) }, null,
            TestContext.Current.CancellationToken);

        Assert.Empty(log.Events);
        Assert.Equal(ErrorCode.Format, outcome.Error);
        Assert.Null(outcome.Reason);
    }
}
