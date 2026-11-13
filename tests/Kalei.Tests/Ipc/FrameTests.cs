using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Kalei.Core.Ipc;
using Xunit;

namespace Kalei.Tests.Ipc;

/// <summary>Framing, serialization and per-message validation of docs/ipc-spec.md v1 (cases 1 to 8 of the table, caps, round trips).</summary>
public sealed class FrameTests
{
    // --- helpers -----------------------------------------------------------------------------------------------------

    internal static byte[] Frame(string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        return Prefixed(body);
    }

    internal static byte[] Prefixed(byte[] body)
    {
        var bytes = new byte[4 + body.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)body.Length);
        body.CopyTo(bytes, 4);
        return bytes;
    }

    internal static byte[] PrefixOnly(uint length)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, length);
        return bytes;
    }

    /// <summary>A valid-looking body of exactly <paramref name="length"/> bytes: the type, then spaces, then the closing brace.</summary>
    private static byte[] Padded(string type, int length)
    {
        var head = $"{{\"type\":\"{type}\"";
        return Encoding.UTF8.GetBytes(head + new string(' ', length - head.Length - 1) + "}");
    }

    private static async Task<IncomingFrame?> ReadAsync(byte[] bytes, Direction direction = Direction.ToCore)
    {
        using var stream = new MemoryStream(bytes);
        return await new FrameReader(stream, direction).ReadAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AssertViolationAsync(byte[] bytes, ProtocolReason reason, ErrorCode code = ErrorCode.Protocol,
        Direction direction = Direction.ToCore)
    {
        var ex = await Assert.ThrowsAsync<IpcException>(() => ReadAsync(bytes, direction));
        Assert.Equal(reason, ex.Reason);
        Assert.Equal(code, ex.Code);
    }

    // --- round trips -------------------------------------------------------------------------------------------------

    public static TheoryData<IpcMessage, Direction> Messages() => new()
    {
        { new HelloMessage { Protocol = 1, Worker = "1.0.0+abc", Modules = [new ModuleInfo { Id = "images", Version = "1.0.0" }] }, Direction.ToCore },
        { new HelloMessage { Protocol = 1 }, Direction.ToWorker },
        { new ProbeMessage { Source = 412, Candidate = "png" }, Direction.ToWorker },
        { new ProbeResultMessage { Format = "png", Confidence = Confidence.Certain, Properties = new ProbeProperties { Width = 4096, Height = 3072, Frames = 1, HasAlpha = true } }, Direction.ToCore },
        { new PreviewMessage { Source = 412, MaxSize = 256 }, Direction.ToWorker },
        { ConvertSample(), Direction.ToWorker },
        { new ProgressMessage { Fraction = 0.42 }, Direction.ToCore },
        { new ProgressMessage { Bytes = 734003200 }, Direction.ToCore },
        { new ProgressMessage(), Direction.ToCore },
        { new ResultMessage { OutputBytes = 1843201, Warnings = [Notice.FirstFrameOnly] }, Direction.ToCore },
        { new ErrorMessage { Code = ErrorCode.Corrupt }, Direction.ToCore },
        { new CancelMessage(), Direction.ToWorker },
        { new CanaryMessage { LocalPort = 50123 }, Direction.ToWorker },
        { new CanaryResultMessage { Local = new CanaryProbe { Outcome = CanaryOutcome.Timeout }, Net = new CanaryProbe { Outcome = CanaryOutcome.Error, Code = 10013 } }, Direction.ToCore },
    };

    internal static ConvertMessage ConvertSample() => new()
    {
        From = "png",
        To = "webp",
        Source = 412,
        Output = 420,
        Options = new Dictionary<string, JsonElement>
        {
            ["quality"] = JsonSerializer.SerializeToElement(82),
            ["stripMetadata"] = JsonSerializer.SerializeToElement(false),
        },
        Limits = new ConvertLimits { Pixels = 250_000_000, Depth = 64, YamlAliases = 100, ExpansionRatio = 100, UncompressedBytes = 1_073_741_824 },
    };

    [Theory]
    [MemberData(nameof(Messages))]
    public async Task Message_WrittenAndRead_IsEqualAndTypeIsTheFirstProperty(IpcMessage message, Direction direction)
    {
        using var stream = new MemoryStream();
        await FrameWriter.WriteAsync(stream, message, TestContext.Current.CancellationToken);
        var bytes = stream.ToArray();

        Assert.StartsWith("{\"type\":\"", Encoding.UTF8.GetString(bytes, 4, 9));
        var frame = await ReadAsync(bytes, direction);

        Assert.NotNull(frame);
        Assert.Equal(message.GetType(), frame.Message.GetType());
        Assert.Equal(Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4), Encoding.UTF8.GetString(FrameWriter.Encode(frame.Message)));
    }

    [Fact]
    public async Task PreviewData_WithPayload_ReturnsThePixels()
    {
        var pixels = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        using var stream = new MemoryStream();
        await FrameWriter.WritePreviewDataAsync(stream, 2, 2, pixels, TestContext.Current.CancellationToken);

        var frame = await ReadAsync(stream.ToArray());

        var header = Assert.IsType<PreviewDataMessage>(frame!.Message);
        Assert.Equal((2, 2, 16), (header.Width, header.Height, header.Length));
        Assert.Equal(pixels, frame.Pixels);
    }

    [Fact]
    public async Task CleanEndOfStreamAtAFrameBoundary_ReturnsNull() => Assert.Null(await ReadAsync([]));

    // --- caps (spec table "Size cap per type") -----------------------------------------------------------------------

    public static TheoryData<string, Direction, int> Caps() => new()
    {
        { "Hello", Direction.ToCore, 4096 },
        { "Hello", Direction.ToWorker, 64 },
        { "Probe", Direction.ToWorker, 256 },
        { "ProbeResult", Direction.ToCore, 512 },
        { "Preview", Direction.ToWorker, 128 },
        { "PreviewData", Direction.ToCore, 256 },
        { "Convert", Direction.ToWorker, 32768 },
        { "Progress", Direction.ToCore, 128 },
        { "Result", Direction.ToCore, 256 },
        { "Error", Direction.ToCore, 128 },
        { "Cancel", Direction.ToWorker, 64 },
        { "Canary", Direction.ToWorker, 128 },
        { "CanaryResult", Direction.ToCore, 256 },
    };

    [Theory]
    [MemberData(nameof(Caps))]
    public async Task Frame_OneByteOverTheCapOfItsType_IsFrameTooLargeForType(string type, Direction direction, int cap)
    {
        var atCap = Prefixed(Padded(type, cap));
        // At the cap the size check passes (the content then fails on its own, which is not the size reason).
        var atCapEx = await Record.ExceptionAsync(() => ReadAsync(atCap, direction));
        Assert.NotEqual(ProtocolReason.FrameTooLargeForType, (atCapEx as IpcException)?.Reason);

        await AssertViolationAsync(Prefixed(Padded(type, cap + 1)), ProtocolReason.FrameTooLargeForType, direction: direction);
    }

    public static TheoryData<IpcMessage, Direction, int> LargestValid() => new()
    {
        { LargestHello(), Direction.ToCore, 4096 },
        { new ProbeMessage { Source = int.MaxValue, Candidate = new string('a', 16) }, Direction.ToWorker, 256 },
        {
            new ProbeResultMessage
            {
                Format = new string('a', 16), Confidence = Confidence.Certain,
                Properties = new ProbeProperties { Width = 1_000_000, Height = 1_000_000, Frames = 100_000, HasAlpha = false, Sheets = 10_000, Encoding = new string('a', 32) },
            },
            Direction.ToCore, 512
        },
        { new PreviewMessage { Source = int.MaxValue, MaxSize = 512 }, Direction.ToWorker, 128 },
        { new PreviewDataMessage { Width = 512, Height = 512, Length = 1_048_576 }, Direction.ToCore, 256 },
        { LargestConvert(), Direction.ToWorker, 32768 },
        { new ProgressMessage { Fraction = 0.12345678901234568 }, Direction.ToCore, 128 },
        { new ProgressMessage { Bytes = 4_294_967_296 }, Direction.ToCore, 128 },
        { new ResultMessage { OutputBytes = 9_007_199_254_740_991, Warnings = [Notice.FirstFrameOnly, Notice.LossyEncoding] }, Direction.ToCore, 256 },
        { new ErrorMessage { Code = ErrorCode.UnsupportedOption }, Direction.ToCore, 128 },
        { new CancelMessage(), Direction.ToWorker, 64 },
        { new CanaryMessage { LocalPort = 65535 }, Direction.ToWorker, 128 },
        {
            new CanaryResultMessage
            {
                Local = new CanaryProbe { Outcome = CanaryOutcome.Error, Code = int.MaxValue },
                Net = new CanaryProbe { Outcome = CanaryOutcome.Error, Code = int.MaxValue },
            },
            Direction.ToCore, 256
        },
    };

    private static HelloMessage LargestHello() => new()
    {
        Protocol = 1,
        Worker = new string('a', 64),
        Modules = [.. Enumerable.Range(0, 16).Select(_ => new ModuleInfo { Id = new string('a', 32), Version = new string('1', 32) })],
    };

    private static ConvertMessage LargestConvert() => new()
    {
        From = new string('a', 16),
        To = new string('a', 16),
        Source = int.MaxValue,
        Output = int.MaxValue,
        Options = Enumerable.Range(0, 32).ToDictionary(i => "k" + new string('a', 28) + (char)('a' + (i % 26)) + (i / 26),
            _ => JsonSerializer.SerializeToElement(new string('v', 256))),
        Limits = new ConvertLimits { Pixels = 500_000_000, Depth = 64, YamlAliases = 100, ExpansionRatio = 100, UncompressedBytes = 1_073_741_824 },
    };

    [Theory]
    [MemberData(nameof(LargestValid))]
    public void LargestValidMessage_IsAtMostHalfTheCap(IpcMessage message, Direction direction, int cap)
    {
        _ = direction;
        var size = FrameWriter.Encode(message).Length;
        Assert.True(size * 2 <= cap, $"{message.GetType().Name}: {size} bytes against a cap of {cap}");
    }

    // --- case 1: length above the protocol maximum -------------------------------------------------------------------

    [Theory]
    [InlineData(1_048_577u)]
    [InlineData(0x80000000u)]
    [InlineData(0xFFFFFFF0u)]
    [InlineData(uint.MaxValue)]
    public Task Length_AboveOneMegabyte_IsFrameTooLarge_BeforeAnyBodyIsRead(uint length) =>
        AssertViolationAsync(PrefixOnly(length), ProtocolReason.FrameTooLarge);

    // --- case 2: length 0 or below the shortest valid body -----------------------------------------------------------

    [Fact]
    public Task Length_Zero_IsBadLength() => AssertViolationAsync(PrefixOnly(0), ProtocolReason.BadLength);

    [Fact]
    public Task Length_BelowTheShortestValidBody_IsBadLength() =>
        AssertViolationAsync([.. PrefixOnly(16), .. new byte[16]], ProtocolReason.BadLength);

    // --- case 4: syntax ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("{\"kind\":\"Progress\"}")]
    [InlineData("{ \"type\":\"Progress\"}")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":}")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":0.1,\"fraction\":0.2}")]
    [InlineData("{\"type\":\"Result\",\"outputBytes\":1,\"warnings\":null}")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":null}")]
    [InlineData("{\"type\":\"Progress\"} ")]
    [InlineData("{\"type\":\"Progress\"}x")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":[[[[[1]]]]]}")]
    [InlineData("{\"type\":\"Progress\"")]
    [InlineData("[\"type\",\"Progress\",\"xxxxxxxxx\"]")]
    public Task Body_NotValidCompactJsonObject_IsBadJson(string json) =>
        AssertViolationAsync(Frame(json), ProtocolReason.BadJson);

    [Fact]
    public Task Body_WithInvalidUtf8_IsBadJson() =>
        AssertViolationAsync(Prefixed([.. "{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"a"u8, 0xFF, .. "\",\"modules\":[]}"u8]), ProtocolReason.BadJson);

    [Fact]
    public Task Body_WithAUtf8ByteOrderMark_IsBadJson() =>
        AssertViolationAsync(Prefixed([0xEF, 0xBB, 0xBF, .. "{\"type\":\"Progress\"}"u8]), ProtocolReason.BadJson);

    // --- case 5: unknown type, or wrong direction --------------------------------------------------------------------

    [Theory]
    [InlineData("{\"type\":\"Bogus\",\"a\":1}")]
    [InlineData("{\"type\":\"progress\"}")]
    [InlineData("{\"type\":\"Convert\",\"from\":\"a\"}")] // a request is not allowed from a Worker
    [InlineData("{\"type\":\"Cancel\"}")]
    [InlineData("{\"type\":\"Probe\",\"source\":1}")]
    public Task Type_UnknownOrNotAllowedFromAWorker_IsUnknownType(string json) =>
        AssertViolationAsync(Frame(json), ProtocolReason.UnknownType);

    [Fact]
    public Task Type_AnAnswerSentToAWorker_IsUnknownType() =>
        AssertViolationAsync(Frame("{\"type\":\"Progress\"}"), ProtocolReason.UnknownType, direction: Direction.ToWorker);

    // --- case 6: unknown field ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("{\"type\":\"Progress\",\"extra\":1}")]
    [InlineData("{\"type\":\"Progress\",\"Fraction\":0.5}")]
    [InlineData("{\"type\":\"Error\",\"code\":\"E_CORRUPT\",\"detail\":\"x\"}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"png\",\"confidence\":\"certain\",\"properties\":{\"colour\":1}}")]
    [InlineData("{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"w\",\"modules\":[{\"id\":\"a\",\"version\":\"1.0.0\",\"x\":1}]}")]
    public Task Field_NotInTheTable_IsUnknownField(string json) =>
        AssertViolationAsync(Frame(json), ProtocolReason.UnknownField);

    // --- case 7: missing field ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("{\"type\":\"Result\"}")]
    [InlineData("{\"type\":\"CanaryResult\"}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"png\",\"confidence\":\"certain\"}")]
    [InlineData("{\"type\":\"Hello\",\"protocol\":1}")]
    [InlineData("{\"type\":\"CanaryResult\",\"local\":{\"outcome\":\"timeout\"}}")]
    public Task Field_Required_IsMissingField(string json) =>
        AssertViolationAsync(Frame(json), ProtocolReason.MissingField);

    // --- case 8: bad value -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("{\"type\":\"Progress\",\"fraction\":2}")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":-0.1}")]
    [InlineData("{\"type\":\"Progress\",\"bytes\":-1}")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":0.5,\"bytes\":10}")]
    [InlineData("{\"type\":\"Progress\",\"fraction\":\"half\"}")]
    [InlineData("{\"type\":\"Progress\",\"bytes\":1.5}")]
    [InlineData("{\"type\":\"Result\",\"outputBytes\":-1}")]
    [InlineData("{\"type\":\"Result\",\"outputBytes\":99999999999999999999}")]
    [InlineData("{\"type\":\"Result\",\"outputBytes\":1,\"warnings\":[\"MISMATCH\"]}")]
    [InlineData("{\"type\":\"Result\",\"outputBytes\":1,\"warnings\":[\"FIRST_FRAME_ONLY\",\"FIRST_FRAME_ONLY\"]}")]
    [InlineData("{\"type\":\"Error\",\"code\":\"E_PROTOCOL\"}")] // a code only the Core may raise
    [InlineData("{\"type\":\"Error\",\"code\":\"E_CRASH\"}")]
    [InlineData("{\"type\":\"Error\",\"code\":\"E_NOPE\"}")]
    [InlineData("{\"type\":\"Error\",\"code\":3}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"PNG\",\"confidence\":\"certain\",\"properties\":{}}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"png\",\"confidence\":\"sure\",\"properties\":{}}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"png\",\"confidence\":\"certain\",\"properties\":{\"width\":0}}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"png\",\"confidence\":\"certain\",\"properties\":{\"width\":1000001}}")]
    [InlineData("{\"type\":\"ProbeResult\",\"format\":\"png\",\"confidence\":\"certain\",\"properties\":{\"encoding\":\"UTF 8\"}}")]
    [InlineData("{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"has space\",\"modules\":[]}")]
    [InlineData("{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"w\",\"modules\":[]}")]
    [InlineData("{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"w\",\"modules\":[{\"id\":\"Bad_Id\",\"version\":\"1.0.0\"}]}")]
    [InlineData("{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"w\",\"modules\":[{\"id\":\"a\",\"version\":\"one\"}]}")]
    [InlineData("{\"type\":\"CanaryResult\",\"local\":{\"outcome\":\"error\"},\"net\":{\"outcome\":\"timeout\"}}")]
    [InlineData("{\"type\":\"CanaryResult\",\"local\":{\"outcome\":\"timeout\",\"code\":1},\"net\":{\"outcome\":\"timeout\"}}")]
    public Task Value_OutOfRangeWrongTypeOrNotAllowed_IsBadValue(string json) =>
        AssertViolationAsync(Frame(json), ProtocolReason.BadValue);

    [Fact]
    public Task Hello_ToTheWorker_WithWorkerFields_IsUnknownField() =>
        AssertViolationAsync(Frame("{\"type\":\"Hello\",\"protocol\":1,\"worker\":\"w\",\"modules\":[]}"), ProtocolReason.UnknownField,
            direction: Direction.ToWorker);

    [Theory]
    [InlineData("{\"type\":\"Probe\",\"source\":0}")]
    [InlineData("{\"type\":\"Probe\",\"source\":1,\"candidate\":\"PNG\"}")]
    [InlineData("{\"type\":\"Preview\",\"source\":1,\"maxSize\":15}")]
    [InlineData("{\"type\":\"Preview\",\"source\":1,\"maxSize\":513}")]
    [InlineData("{\"type\":\"Canary\",\"localPort\":1023}")]
    [InlineData("{\"type\":\"Canary\",\"localPort\":65536}")]
    public Task Request_WithABadValue_IsBadValue_WhenTheWorkerReadsIt(string json) =>
        AssertViolationAsync(Frame(json), ProtocolReason.BadValue, direction: Direction.ToWorker);

    [Theory]
    [InlineData("\"options\":{\"quality\":[1]}")]
    [InlineData("\"options\":{\"quality\":null}")]
    [InlineData("\"options\":{\"quality\":1.5}")]
    [InlineData("\"options\":{\"bad key\":1}")]
    public async Task Convert_WithABadOption_IsRejected(string options)
    {
        var json = "{\"type\":\"Convert\",\"from\":\"png\",\"to\":\"webp\",\"source\":4,\"output\":5," + options +
            ",\"limits\":{\"pixels\":1,\"depth\":1,\"yamlAliases\":1,\"expansionRatio\":1,\"uncompressedBytes\":1}}";
        var ex = await Assert.ThrowsAsync<IpcException>(() => ReadAsync(Frame(json), Direction.ToWorker));
        Assert.Contains(ex.Reason, new[] { ProtocolReason.BadValue, ProtocolReason.BadJson });
    }

    [Theory]
    [InlineData(0, 2, 0)]
    [InlineData(2, 2, 15)] // length is not width x height x 4
    [InlineData(513, 1, 2052)] // above the preview size
    public Task PreviewData_WithAnInconsistentHeader_IsBadValue_BeforeThePayloadIsRead(int width, int height, int length) =>
        AssertViolationAsync(Frame($"{{\"type\":\"PreviewData\",\"width\":{width},\"height\":{height},\"length\":{length}}}"),
            ProtocolReason.BadValue);

    // --- case 12 at the reader: the pipe closes in the middle of something -------------------------------------------

    [Fact]
    public Task EndOfStream_InsideThePrefix_IsPipeClosedAndE_Crash() =>
        AssertViolationAsync([1, 0], ProtocolReason.PipeClosed, ErrorCode.Crash);

    [Fact]
    public Task EndOfStream_InsideTheBody_IsPipeClosedAndE_Crash() =>
        AssertViolationAsync([.. PrefixOnly(100), .. "{\"type\":\"Progress\""u8], ProtocolReason.PipeClosed, ErrorCode.Crash);

    [Fact]
    public Task EndOfStream_InsideThePreviewPayload_IsPipeClosedAndE_Crash() =>
        AssertViolationAsync([.. Frame("{\"type\":\"PreviewData\",\"width\":2,\"height\":2,\"length\":16}"), .. new byte[7]],
            ProtocolReason.PipeClosed, ErrorCode.Crash);

    // --- the writer refuses to exceed the cap (a programming error, not a protocol error) ----------------------------

    [Fact]
    public async Task Writer_MessageOverItsCap_Throws()
    {
        var huge = new HelloMessage
        {
            Protocol = 1,
            Worker = new string('a', 5000),
            Modules = [new ModuleInfo { Id = "a", Version = "1.0.0" }],
        };
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() => FrameWriter.WriteAsync(stream, huge, TestContext.Current.CancellationToken));
        Assert.Equal(0, stream.Length);
    }
}
