# Minimal LSA secret access for the temporary auto-logon of W7 (same mechanism as Sysinternals Autologon:
# the password lives in the LSA secret "DefaultPassword", never in the registry in clear text).
# Needs an elevated process. Dot-source this file; it defines [KaleiLsa] with Set / Delete / Exists.
if (-not ('KaleiLsa' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class KaleiLsa
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_UNICODE_STRING { public ushort Length; public ushort MaximumLength; public IntPtr Buffer; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_OBJECT_ATTRIBUTES
    {
        public int Length; public IntPtr RootDirectory; public IntPtr ObjectName; public uint Attributes;
        public IntPtr SecurityDescriptor; public IntPtr SecurityQualityOfService;
    }

    [DllImport("advapi32.dll")] private static extern uint LsaOpenPolicy(IntPtr systemName, ref LSA_OBJECT_ATTRIBUTES attrs, uint access, out IntPtr policy);
    [DllImport("advapi32.dll")] private static extern uint LsaStorePrivateData(IntPtr policy, ref LSA_UNICODE_STRING keyName, IntPtr privateData);
    [DllImport("advapi32.dll")] private static extern uint LsaRetrievePrivateData(IntPtr policy, ref LSA_UNICODE_STRING keyName, out IntPtr privateData);
    [DllImport("advapi32.dll")] private static extern uint LsaClose(IntPtr policy);
    [DllImport("advapi32.dll")] private static extern uint LsaFreeMemory(IntPtr buffer);
    [DllImport("advapi32.dll")] private static extern int LsaNtStatusToWinError(uint status);

    private const uint POLICY_GET_PRIVATE_INFORMATION = 0x4;
    private const uint POLICY_CREATE_SECRET = 0x20;
    private const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
    private const string KeyNameText = "DefaultPassword";

    private static LSA_UNICODE_STRING Make(string s, out IntPtr mem)
    {
        mem = Marshal.StringToHGlobalUni(s);
        LSA_UNICODE_STRING u;
        u.Length = (ushort)(s.Length * 2);
        u.MaximumLength = (ushort)(s.Length * 2 + 2);
        u.Buffer = mem;
        return u;
    }

    private static IntPtr Open(uint access)
    {
        LSA_OBJECT_ATTRIBUTES a = new LSA_OBJECT_ATTRIBUTES();
        a.Length = Marshal.SizeOf(typeof(LSA_OBJECT_ATTRIBUTES));
        IntPtr h;
        uint st = LsaOpenPolicy(IntPtr.Zero, ref a, access, out h);
        if (st != 0) throw new InvalidOperationException("LsaOpenPolicy error " + LsaNtStatusToWinError(st));
        return h;
    }

    public static void Set(string value)
    {
        IntPtr h = Open(POLICY_CREATE_SECRET);
        IntPtr km = IntPtr.Zero, vm = IntPtr.Zero, pv = IntPtr.Zero;
        try
        {
            LSA_UNICODE_STRING key = Make(KeyNameText, out km);
            LSA_UNICODE_STRING val = Make(value, out vm);
            pv = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_UNICODE_STRING)));
            Marshal.StructureToPtr(val, pv, false);
            uint st = LsaStorePrivateData(h, ref key, pv);
            if (st != 0) throw new InvalidOperationException("LsaStorePrivateData error " + LsaNtStatusToWinError(st));
        }
        finally
        {
            if (vm != IntPtr.Zero) { for (int i = 0; i < value.Length * 2; i++) Marshal.WriteByte(vm, i, 0); Marshal.FreeHGlobal(vm); }
            if (km != IntPtr.Zero) Marshal.FreeHGlobal(km);
            if (pv != IntPtr.Zero) Marshal.FreeHGlobal(pv);
            LsaClose(h);
        }
    }

    public static void Delete()
    {
        IntPtr h = Open(POLICY_CREATE_SECRET);
        IntPtr km = IntPtr.Zero;
        try
        {
            LSA_UNICODE_STRING key = Make(KeyNameText, out km);
            uint st = LsaStorePrivateData(h, ref key, IntPtr.Zero);
            if (st != 0 && st != STATUS_OBJECT_NAME_NOT_FOUND) throw new InvalidOperationException("LsaStorePrivateData(delete) error " + LsaNtStatusToWinError(st));
        }
        finally { if (km != IntPtr.Zero) Marshal.FreeHGlobal(km); LsaClose(h); }
    }

    public static bool Exists()
    {
        IntPtr h = Open(POLICY_GET_PRIVATE_INFORMATION);
        IntPtr km = IntPtr.Zero;
        try
        {
            LSA_UNICODE_STRING key = Make(KeyNameText, out km);
            IntPtr data;
            uint st = LsaRetrievePrivateData(h, ref key, out data);
            if (st == 0) { if (data != IntPtr.Zero) LsaFreeMemory(data); return true; }
            if (st == STATUS_OBJECT_NAME_NOT_FOUND) return false;
            throw new InvalidOperationException("LsaRetrievePrivateData error " + LsaNtStatusToWinError(st));
        }
        finally { if (km != IntPtr.Zero) Marshal.FreeHGlobal(km); LsaClose(h); }
    }
}
'@
}
