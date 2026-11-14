using System.IO.Pipes;
using System.Runtime.InteropServices;
using Kalei.Core.Platform;
using Microsoft.Win32.SafeHandles;

namespace Kalei.Platform.Windows.Sandbox;

/// <summary>
/// Starts a Worker in an AppContainer with no capabilities (no network, no files of its own) inside a Job Object (memory cap, CPU cap,
/// one process, killed when the Core lets go of it), with inherited anonymous pipes and only the handles it is given (ADR 0003, S1, S5).
/// If any step fails nothing runs: a Worker never starts unsandboxed.
/// </summary>
public sealed class WindowsSandboxLauncher : ISandboxLauncher
{
    // A handle is inheritable only while CreateProcess runs, and only one launch at a time makes handles inheritable.
    private static readonly Lock s_inheritance = new();

    private readonly SandboxSetup _setup;
    private readonly string _workerPath;
    private readonly string[] _leadingArguments;
    private readonly Lazy<SandboxSetupStatus> _prepared;

    /// <param name="setup">The first-run set-up; run once, on the first Start. Anything but Ready means no Worker starts.</param>
    /// <param name="workerPath">The Worker executable.</param>
    /// <param name="leadingArguments">Arguments before the two pipe handles; no spaces or quotes.</param>
    public WindowsSandboxLauncher(SandboxSetup setup, string workerPath, params string[] leadingArguments)
    {
        if (leadingArguments.Any(a => a.Length == 0 || a.AsSpan().IndexOfAny(" \t\"") >= 0))
        {
            throw new ArgumentException("Arguments cannot be empty or hold spaces or quotes.", nameof(leadingArguments));
        }

        _setup = setup;
        _workerPath = Path.GetFullPath(workerPath);
        _leadingArguments = leadingArguments;
        _prepared = new Lazy<SandboxSetupStatus>(setup.Prepare);
    }

    public IWorkerProcess Start(WorkerStart start)
    {
        if (_prepared.Value != SandboxSetupStatus.Ready)
        {
            throw new SandboxStartException(SandboxFailure.Setup);
        }

        SafeAppContainerSid sid;
        try
        {
            sid = AppContainerProfile.Ensure(_setup.ProfileName);
        }
        catch (COMException e)
        {
            throw new SandboxStartException(SandboxFailure.Setup, e.HResult);
        }

        using (sid)
        {
            return StartWith(sid, start);
        }
    }

    private SandboxedWorker StartWith(SafeAppContainerSid sid, WorkerStart start)
    {
        AnonymousPipeServerStream? toWorker = null;
        AnonymousPipeServerStream? fromWorker = null;
        try
        {
            try
            {
                toWorker = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);
                fromWorker = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
            }
            catch (IOException e)
            {
                throw new SandboxStartException(SandboxFailure.Pipes, e.HResult);
            }

            // The Worker reads from the pipe the Core writes to, and writes to the one the Core reads from.
            List<SafeHandle> inherited = [toWorker.ClientSafePipeHandle, fromWorker.ClientSafePipeHandle, .. start.InheritedHandles];
            var commandLine = $"\"{_workerPath}\" {string.Join(' ', [.. _leadingArguments, Text(inherited[0]), Text(inherited[1])])}";

            var (process, thread) = CreateSuspendedProcess(sid, inherited, commandLine);
            SafeKernelHandle? job = null;
            SafeKernelHandle? port = null;
            try
            {
                (job, port) = CreateJob(start);
                if (!Native.AssignProcessToJobObject(job, process))
                {
                    throw new SandboxStartException(SandboxFailure.Job, Marshal.GetLastWin32Error());
                }

                if (Native.ResumeThread(thread) == uint.MaxValue)
                {
                    throw new SandboxStartException(SandboxFailure.Process, Marshal.GetLastWin32Error());
                }
            }
            catch
            {
                Native.TerminateProcess(process, 1);
                job?.Dispose();
                port?.Dispose();
                process.Dispose();
                throw;
            }
            finally
            {
                thread.Dispose();
            }

            toWorker.DisposeLocalCopyOfClientHandle();
            fromWorker.DisposeLocalCopyOfClientHandle();
            return new SandboxedWorker(toWorker, fromWorker, process, job, port, start.MemoryLimitBytes);
        }
        catch
        {
            toWorker?.Dispose();
            fromWorker?.Dispose();
            throw;
        }
    }

    private static string Text(SafeHandle handle) => handle.DangerousGetHandle().ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);

    private (SafeProcessHandle Process, SafeKernelHandle Thread) CreateSuspendedProcess(SafeAppContainerSid sid, List<SafeHandle> inherited, string commandLine)
    {
        var attributeList = nint.Zero;
        var handleArray = nint.Zero;
        var capabilities = nint.Zero;
        var command = nint.Zero;
        var environment = nint.Zero;
        try
        {
            // Two attributes: the list of handles to inherit, and the AppContainer with no capabilities.
            nuint size = 0;
            Native.InitializeProcThreadAttributeList(0, 2, 0, ref size); // fails by design: it only reports the size
            attributeList = Marshal.AllocHGlobal((nint)size);
            if (!Native.InitializeProcThreadAttributeList(attributeList, 2, 0, ref size))
            {
                throw new SandboxStartException(SandboxFailure.Process, Marshal.GetLastWin32Error());
            }

            handleArray = Marshal.AllocHGlobal(nint.Size * inherited.Count);
            for (var i = 0; i < inherited.Count; i++)
            {
                Marshal.WriteIntPtr(handleArray, i * nint.Size, inherited[i].DangerousGetHandle());
            }

            if (!Native.UpdateProcThreadAttribute(attributeList, 0, Native.ProcThreadAttributeHandleList, handleArray, (nuint)(nint.Size * inherited.Count), 0, 0))
            {
                throw new SandboxStartException(SandboxFailure.Handles, Marshal.GetLastWin32Error());
            }

            capabilities = Marshal.AllocHGlobal(Marshal.SizeOf<SecurityCapabilities>());
            Marshal.StructureToPtr(new SecurityCapabilities { AppContainerSid = sid.DangerousGetHandle() }, capabilities, fDeleteOld: false);
            if (!Native.UpdateProcThreadAttribute(attributeList, 0, Native.ProcThreadAttributeSecurityCapabilities, capabilities,
                    (nuint)Marshal.SizeOf<SecurityCapabilities>(), 0, 0))
            {
                throw new SandboxStartException(SandboxFailure.Process, Marshal.GetLastWin32Error());
            }

            command = Marshal.StringToHGlobalUni(commandLine);
            environment = Marshal.StringToHGlobalUni(EnvironmentBlock());
            var info = new StartupInfoEx { AttributeList = attributeList };
            info.StartupInfo.Cb = Marshal.SizeOf<StartupInfoEx>();

            ProcessInformation created;
            bool ok;
            int error;
            lock (s_inheritance)
            {
                foreach (var handle in inherited)
                {
                    if (!Native.SetHandleInformation(handle, Native.HandleFlagInherit, Native.HandleFlagInherit))
                    {
                        throw new SandboxStartException(SandboxFailure.Handles, Marshal.GetLastWin32Error());
                    }
                }

                try
                {
                    ok = Native.CreateProcess(_workerPath, command, 0, 0, true,
                        Native.ExtendedStartupInfoPresent | Native.CreateSuspended | Native.CreateNoWindow | Native.CreateUnicodeEnvironment,
                        environment, Path.GetDirectoryName(_workerPath)!, ref info, out created);
                    error = Marshal.GetLastWin32Error();
                }
                finally
                {
                    foreach (var handle in inherited)
                    {
                        Native.SetHandleInformation(handle, Native.HandleFlagInherit, 0);
                    }
                }
            }

            if (!ok)
            {
                throw new SandboxStartException(SandboxFailure.Process, error);
            }

            return (new SafeProcessHandle(created.Process, ownsHandle: true), new SafeKernelHandle(created.Thread));
        }
        finally
        {
            if (attributeList != 0)
            {
                Native.DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }

            FreeIfSet(handleArray);
            FreeIfSet(capabilities);
            FreeIfSet(command);
            FreeIfSet(environment);
        }
    }

    private static void FreeIfSet(nint memory)
    {
        if (memory != 0)
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    /// <summary>
    /// The Worker gets a few variables and nothing of the user's environment. Without a profile variable CreateProcess fails for an
    /// AppContainer with ERROR_ENVVAR_NOT_FOUND (203, measured); they point to a folder the sandbox cannot use.
    /// </summary>
    private static string EnvironmentBlock()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var nowhere = Path.Combine(root, "Temp");
        return $"LOCALAPPDATA={nowhere}\0PATH={Path.Combine(root, "system32")}\0SystemRoot={root}\0TEMP={nowhere}\0TMP={nowhere}\0windir={root}\0\0";
    }

    private static (SafeKernelHandle Job, SafeKernelHandle Port) CreateJob(WorkerStart start)
    {
        var job = Native.CreateJobObject(0, 0);
        if (job.IsInvalid)
        {
            throw new SandboxStartException(SandboxFailure.Job, Marshal.GetLastWin32Error());
        }

        var port = Native.CreateIoCompletionPort(-1, 0, 0, 1);
        try
        {
            if (port.IsInvalid)
            {
                throw new SandboxStartException(SandboxFailure.Job, Marshal.GetLastWin32Error());
            }

            var association = new JobCompletionPort { CompletionKey = 1, CompletionPort = port.DangerousGetHandle() };
            if (!Native.SetInformationJobObject(job, Native.JobObjectAssociateCompletionPort, ref association, Marshal.SizeOf<JobCompletionPort>()))
            {
                throw new SandboxStartException(SandboxFailure.Job, Marshal.GetLastWin32Error());
            }

            var limits = new JobExtendedLimit
            {
                Basic =
                {
                    LimitFlags = Native.JobLimitKillOnJobClose | Native.JobLimitActiveProcess | Native.JobLimitProcessMemory
                        | Native.JobLimitJobTime | Native.JobLimitDieOnUnhandledException,
                    ActiveProcessLimit = 1,
                    PerJobUserTimeLimit = start.CpuTimeLimit.Ticks, // 100 ns units, the same as ticks
                },
                ProcessMemoryLimit = checked((nuint)start.MemoryLimitBytes),
            };
            if (!Native.SetInformationJobObject(job, Native.JobObjectExtendedLimitInformation, ref limits, Marshal.SizeOf<JobExtendedLimit>()))
            {
                throw new SandboxStartException(SandboxFailure.Job, Marshal.GetLastWin32Error());
            }

            return (job, port);
        }
        catch
        {
            job.Dispose();
            port.Dispose();
            throw;
        }
    }
}
