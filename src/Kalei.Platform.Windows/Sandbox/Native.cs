using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Kalei.Platform.Windows.Sandbox;

/// <summary>A SID returned by the AppContainer profile functions; freed with FreeSid.</summary>
internal sealed class SafeAppContainerSid : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeAppContainerSid()
        : base(ownsHandle: true)
    {
    }

    public SecurityIdentifier ToSecurityIdentifier() => new(handle);

    protected override bool ReleaseHandle() => Native.FreeSid(handle) == 0;
}

internal static partial class Native
{
    public const int HresultAlreadyExists = unchecked((int)0x800700B7);
    public const int HresultNotFound = unchecked((int)0x80070490);

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int CreateAppContainerProfile(string name, string displayName, string description, nint capabilities, uint capabilityCount,
        out SafeAppContainerSid sid);

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DeriveAppContainerSidFromAppContainerName(string name, out SafeAppContainerSid sid);

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DeleteAppContainerProfile(string name);

    [LibraryImport("advapi32.dll")]
    public static partial nint FreeSid(nint sid);
}
