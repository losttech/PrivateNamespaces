namespace LostTech.Win32.PrivateNamespaces;

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

using Vanara.PInvoke;

/// <summary>
/// A named scope for shared objects such as
/// <see cref="System.Threading.Mutex"/> that provides access control.
/// </summary>
public sealed class PrivateNamespace: IDisposable {
    readonly SafeNamespaceHandle handle;
    readonly bool destroyOnClose;

    public PrivateNamespace(BoundaryDescriptor boundary, string alias, bool destroyOnClose)
        : this(CreateOrThrow(security: null, boundary, alias), destroyOnClose) { }

    public PrivateNamespace(ObjectSecurity security, BoundaryDescriptor boundary, string alias, bool destroyOnClose)
        : this(CreateOrThrow(security: security ?? throw new ArgumentNullException(nameof(security)), boundary, alias), destroyOnClose) { }

    public static PrivateNamespace Open(BoundaryDescriptor boundary, string name) {
        var handle = OpenPrivateNamespaceInternal(boundary.handle, name);
        GC.KeepAlive(boundary);
        if (handle.IsInvalid)
            throw new Win32Exception();
        return new(handle, destroyOnClose: false);
    }

    static unsafe SafeNamespaceHandle CreateOrThrow(ObjectSecurity? security, BoundaryDescriptor boundary, string alias) {
        if (boundary is null) throw new ArgumentNullException(nameof(boundary));
        if (alias is null) throw new ArgumentNullException(nameof(alias));

        byte[]? securityBytes = security?.GetSecurityDescriptorBinaryForm();

        fixed (byte* securityPtr = securityBytes) {
            var securityAttributes = new SECURITY_ATTRIBUTES {
                lpSecurityDescriptor = new((IntPtr)securityPtr),
            };
            var handle = CreatePrivateNamespace(securityAttributes, boundary.handle, alias);
            GC.KeepAlive(boundary);
            if (handle.IsInvalid)
                throw new Win32Exception();
            return handle;
        }
    }

    PrivateNamespace(SafeNamespaceHandle handle, bool destroyOnClose) {
        this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
        this.destroyOnClose = destroyOnClose;
    }

    public void Dispose() {
        if (this.handle.IsClosed) return;

        if (!ClosePrivateNamespace(this.handle,
                                   this.destroyOnClose ? PRIVATE_NAMESPACE_FLAG_DESTROY : 0))
            throw new Win32Exception();

        this.handle.SetHandleAsInvalid();
    }

    /// <summary>Opens a private namespace.</summary>
    /// <param name="lpBoundaryDescriptor">
    /// A descriptor that defines how the namespace is to be isolated.
    /// The <c>CreateBoundaryDescriptor</c> function creates a boundary descriptor.
    /// </param>
    /// <param name="lpAliasPrefix">
    /// The prefix for the namespace. To create an object in this namespace,
    /// specify the object name as prefix\objectname.
    /// </param>
    /// <returns>The function returns the handle to the existing namespace.</returns>
    [DllImport(Lib.Kernel32, SetLastError = true, CharSet = CharSet.Unicode,
               EntryPoint = "OpenPrivateNamespaceW")]
    [PInvokeData("WinBase.h", MSDNShortId = "ms684318")]
    static extern SafeNamespaceHandle OpenPrivateNamespaceInternal(
        BoundaryDescriptorHandle lpBoundaryDescriptor, string lpAliasPrefix);
}