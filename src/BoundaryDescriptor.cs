namespace LostTech.Win32.PrivateNamespaces;

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

using Vanara.PInvoke;

public sealed class BoundaryDescriptor: IDisposable {
    internal SafeBoundaryDescriptorHandle handle;

    BoundaryDescriptor(SafeBoundaryDescriptorHandle handle) {
        this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    public BoundaryDescriptor(string name, CreateFlags flags)
        : this(CreateOrThrow(name, flags)) { }

    static SafeBoundaryDescriptorHandle CreateOrThrow(string name, CreateFlags flags) {
        var handle = CreateBoundaryDescriptor(name, (uint)flags);
        if (handle.IsInvalid)
            throw new Win32Exception();
        return handle;
    }

    public void Add(SecurityIdentifier identifier) {
        if (identifier is null) throw new ArgumentNullException(nameof(identifier));

        byte[] bytes = new byte[identifier.BinaryLength];
        identifier.GetBinaryForm(bytes, 0);

        unsafe {
            fixed (byte* ptr = bytes) {
                this.Add(new PSID((IntPtr)ptr));
            }
        }
    }

    public void Add(WellKnownSidType sidType)
        => this.Add(new SecurityIdentifier(sidType, domainSid: null));

    void Add(PSID sid) {
        if (!Add(this.handle, sid, out var newHandle))
            throw new Win32Exception();
        this.handle.SetHandleAsInvalid();
        this.handle = new SafeBoundaryDescriptorHandle(newHandle.DangerousGetHandle());
    }

    static bool Add(BoundaryDescriptorHandle handle, PSID sid, out BoundaryDescriptorHandle newHandle) {
        newHandle = handle;
        return Marshal.ReadByte(sid.DangerousGetHandle(), 7) == 16
            ? AddIntegrityLabelToBoundaryDescriptor(ref newHandle, sid)
            : AddSIDToBoundaryDescriptor(ref newHandle, sid);
    }

    public void Dispose() => this.handle.Dispose();

    public enum CreateFlags: uint {
        None = 0,
        AddAppContainerSID = 1,
    }
}