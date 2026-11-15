using System.Runtime.InteropServices;

namespace Kalei.Platform.Windows.Sandbox;

/// <summary>The AppContainer profile of a Worker executable: created by the app at first run, removed by the clean-up command (release-pipeline.md).</summary>
internal static class AppContainerProfile
{
    /// <summary>The profile of the built-in Worker (one profile per Worker executable).</summary>
    public const string BuiltinWorker = "Kalei.Worker.Builtin";

    /// <summary>Creates the profile, or takes the SID of the one that exists. The profile has no capabilities: that is what blocks the network (ADR 0003).</summary>
    public static SafeAppContainerSid Ensure(string name)
    {
        var hr = Native.CreateAppContainerProfile(name, "Kalei converter", "Sandbox of a Kalei converter. No capabilities: no network, no files of its own.", 0, 0, out var sid);
        if (hr == Native.HresultAlreadyExists)
        {
            hr = Native.DeriveAppContainerSidFromAppContainerName(name, out sid);
        }

        Marshal.ThrowExceptionForHR(hr);
        return sid;
    }

    /// <summary>True if the profile is gone afterwards (it did not exist, or it was removed).</summary>
    public static bool Delete(string name)
    {
        var hr = Native.DeleteAppContainerProfile(name);
        return hr >= 0 || hr == Native.HresultNotFound;
    }
}
