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

/// <summary>A kernel object handle (job, completion port, thread) closed with CloseHandle.</summary>
internal sealed class SafeKernelHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeKernelHandle()
        : base(ownsHandle: true)
    {
    }

    public SafeKernelHandle(nint existing)
        : base(ownsHandle: true) => SetHandle(existing);

    protected override bool ReleaseHandle() => Native.CloseHandle(handle);
}

[StructLayout(LayoutKind.Sequential)]
internal struct SecurityCapabilities
{
    public nint AppContainerSid;
    public nint Capabilities;
    public uint CapabilityCount;
    public uint Reserved;
}

[StructLayout(LayoutKind.Sequential)]
internal struct StartupInfo
{
    public int Cb;
    public nint Reserved;
    public nint Desktop;
    public nint Title;
    public int X;
    public int Y;
    public int XSize;
    public int YSize;
    public int XCountChars;
    public int YCountChars;
    public int FillAttribute;
    public int Flags;
    public short ShowWindow;
    public short Reserved2Size;
    public nint Reserved2;
    public nint StdInput;
    public nint StdOutput;
    public nint StdError;
}

[StructLayout(LayoutKind.Sequential)]
internal struct StartupInfoEx
{
    public StartupInfo StartupInfo;
    public nint AttributeList;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ProcessInformation
{
    public nint Process;
    public nint Thread;
    public int ProcessId;
    public int ThreadId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JobBasicLimit
{
    public long PerProcessUserTimeLimit;
    public long PerJobUserTimeLimit;
    public uint LimitFlags;
    public nuint MinimumWorkingSetSize;
    public nuint MaximumWorkingSetSize;
    public uint ActiveProcessLimit;
    public nuint Affinity;
    public uint PriorityClass;
    public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JobExtendedLimit
{
    public JobBasicLimit Basic;
    public ulong IoReadOps;
    public ulong IoWriteOps;
    public ulong IoOtherOps;
    public ulong IoReadBytes;
    public ulong IoWriteBytes;
    public ulong IoOtherBytes;
    public nuint ProcessMemoryLimit;
    public nuint JobMemoryLimit;
    public nuint PeakProcessMemoryUsed;
    public nuint PeakJobMemoryUsed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JobCompletionPort
{
    public nint CompletionKey;
    public nint CompletionPort;
}

internal static partial class Native
{
    public const int HresultAlreadyExists = unchecked((int)0x800700B7);
    public const int HresultNotFound = unchecked((int)0x80070490);

    public const uint ExtendedStartupInfoPresent = 0x00080000;
    public const uint CreateSuspended = 0x00000004;
    public const uint CreateNoWindow = 0x08000000;
    public const uint CreateUnicodeEnvironment = 0x00000400;
    public const uint HandleFlagInherit = 1;

    public const nuint ProcThreadAttributeHandleList = 0x00020002;
    public const nuint ProcThreadAttributeSecurityCapabilities = 0x00020009;

    public const uint JobLimitJobTime = 0x4;
    public const uint JobLimitActiveProcess = 0x8;
    public const uint JobLimitProcessMemory = 0x100;
    public const uint JobLimitDieOnUnhandledException = 0x400;
    public const uint JobLimitKillOnJobClose = 0x2000;
    public const int JobObjectAssociateCompletionPort = 7;
    public const int JobObjectExtendedLimitInformation = 9;

    public const uint JobMsgEndOfJobTime = 1;
    public const uint JobMsgProcessMemoryLimit = 9;
    public const uint JobMsgJobMemoryLimit = 10;

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int CreateAppContainerProfile(string name, string displayName, string description, nint capabilities, uint capabilityCount,
        out SafeAppContainerSid sid);

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DeriveAppContainerSidFromAppContainerName(string name, out SafeAppContainerSid sid);

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DeleteAppContainerProfile(string name);

    [LibraryImport("advapi32.dll")]
    public static partial nint FreeSid(nint sid);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetHandleInformation(SafeHandle handle, uint mask, uint flags);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InitializeProcThreadAttributeList(nint list, int count, int flags, ref nuint size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateProcThreadAttribute(nint list, uint flags, nuint attribute, nint value, nuint size, nint previous, nint returned);

    [LibraryImport("kernel32.dll")]
    public static partial void DeleteProcThreadAttributeList(nint list);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateProcess(string applicationName, nint commandLine, nint processAttributes, nint threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles, uint creationFlags, nint environment, string currentDirectory,
        ref StartupInfoEx startupInfo, out ProcessInformation processInformation);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", SetLastError = true)]
    public static partial SafeKernelHandle CreateJobObject(nint attributes, nint name);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetInformationJobObject(SafeKernelHandle job, int informationClass, ref JobExtendedLimit information, int length);

    [LibraryImport("kernel32.dll", EntryPoint = "SetInformationJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetInformationJobObject(SafeKernelHandle job, int informationClass, ref JobCompletionPort information, int length);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool QueryInformationJobObject(SafeKernelHandle job, int informationClass, out JobExtendedLimit information, int length, nint returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AssignProcessToJobObject(SafeKernelHandle job, SafeHandle process);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateJobObject(SafeKernelHandle job, uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint ResumeThread(SafeHandle thread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateProcess(SafeHandle process, uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial SafeKernelHandle CreateIoCompletionPort(nint fileHandle, nint existingPort, nuint key, uint concurrentThreads);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetQueuedCompletionStatus(SafeKernelHandle port, out uint message, out nuint key, out nint overlapped, uint milliseconds);
}
