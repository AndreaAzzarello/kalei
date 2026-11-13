using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Kalei.Tests.Dependencies;

#pragma warning disable SYSLIB1054, CA5392, CA1401, CA1810, CA1822, CA2000, CA1812, CA1416

/// <summary>
/// Deliberately breaks every dependency rule so the scanner tests can prove it sees each one.
/// Never referenced by product code; it only lives in the test assembly.
/// </summary>
internal sealed class ForbiddenFixture
{
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    public object UsesProcess() => Process.GetCurrentProcess();

    public object UsesHttp() => new HttpClient();

    public object UsesPipes() => new AnonymousPipeServerStream();

    public object UsesRegistry() => Registry.CurrentUser;

    public uint UsesNativeCall() => GetCurrentProcessId();

    /// <summary>Allowed by the Core rule: cross-platform file handles (Broker, ticket 08).</summary>
    public object UsesSafeHandle() => new SafeFileHandle();
}
