namespace LostTech.Win32.PrivateNamespaces;

using System.Runtime.InteropServices;

using Vanara.PInvoke;

static class NativeMethods {
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern PrivateNamespace CreatePrivateNamespaceW(
        [In, Optional]
        in SECURITY_ATTRIBUTES security,
        [In]
        BoundaryDescriptor boundary,
        string alias);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClosePrivateNamespace(
        PrivateNamespace ns,
        ClosePrivateNamespaceFlags flags);

    [Flags]
    public enum ClosePrivateNamespaceFlags {
        None = 0,
        Destroy = 1,
    }
}
