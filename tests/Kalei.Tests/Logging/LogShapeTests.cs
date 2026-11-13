using Kalei.Core.Logging;
using Xunit;

namespace Kalei.Tests.Logging;

public sealed class LogShapeTests
{
    // --- the rule itself: each way of smuggling a path in must be caught ----------------------------------------------

    [Theory]
    [InlineData(typeof(TakesString))]
    [InlineData(typeof(TakesUri))]
    [InlineData(typeof(TakesFileInfo))]
    [InlineData(typeof(TakesObject))]
    [InlineData(typeof(TakesException))]
    [InlineData(typeof(TakesCharSpan))]
    [InlineData(typeof(ExposesStringProperty))]
    [InlineData(typeof(TakesListOfStrings))]
    public void Rule_CatchesFreeTextAndPathTypes(Type bad) =>
        Assert.NotEmpty(LogShapeRules.Violations([bad]));

    [Fact]
    public void Rule_AcceptsTypedFieldsOnly() =>
        Assert.Empty(LogShapeRules.Violations([typeof(TypedOnly)]));

    // --- the real logging types ---------------------------------------------------------------------------------------

    [Fact]
    public void LoggingTypes_CannotBeGivenAPathOrFileName()
    {
        var loggingTypes = typeof(IKaleiLog).Assembly.GetTypes()
            .Where(t => t.IsPublic && t.Namespace == "Kalei.Core.Logging")
            .ToList();

        Assert.Contains(typeof(IKaleiLog), loggingTypes);
        Assert.Contains(typeof(LogEvent), loggingTypes);
        Assert.Empty(LogShapeRules.Violations(loggingTypes));
    }

    [Fact]
    public void LogEvent_HoldsLevelCodeAndDuration()
    {
        var logEvent = new LogEvent(LogLevel.Warning, LogEventCode.NetCanary, TimeSpan.FromMilliseconds(12));

        Assert.Equal(LogLevel.Warning, logEvent.Level);
        Assert.Equal(LogEventCode.NetCanary, logEvent.Code);
        Assert.Equal(TimeSpan.FromMilliseconds(12), logEvent.Duration);
        Assert.Null(new LogEvent(LogLevel.Error, LogEventCode.NetCanary).Duration);
    }

#pragma warning disable CA1812, CA1822, IDE0060
    private sealed class TakesString { public TakesString(string path) { } }

    private sealed class TakesUri { public void Write(Uri uri) { } }

    private sealed class TakesFileInfo { public void Write(FileInfo file) { } }

    private sealed class TakesObject { public void Write(object value) { } }

    private sealed class TakesException { public void Write(Exception error) { } }

    private sealed class TakesCharSpan { public void Write(ReadOnlySpan<char> text) { } }

    private sealed class ExposesStringProperty { public string Name { get; } = string.Empty; }

    private sealed class TakesListOfStrings { public void Write(IReadOnlyList<string> lines) { } }

    private sealed class TypedOnly { public void Write(int code, TimeSpan duration, LogLevel level) { } }
#pragma warning restore CA1812, CA1822, IDE0060
}
