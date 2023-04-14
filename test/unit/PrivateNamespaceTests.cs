namespace LostTech.Win32.PrivateNamespaces;

using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Vanara.PInvoke;

using Xunit;

using static BoundaryDescriptor;

public class PrivateNamespaceTests {
    [Fact]
    public void Use() {
        using var boundary =
            new BoundaryDescriptor(Guid.NewGuid().ToString(), CreateFlags.None);
        string name = Guid.NewGuid().ToString();
        using var ns = new PrivateNamespace(boundary, name, destroyOnClose: true);
        using var sharedObject = new Mutex(initiallyOwned: true, $"{name}\\{Guid.NewGuid()}");
    }

    [Fact(Skip = "Requires manual interaction, does not work yet")]
    public void ShareWithAppContainer() {
        const string packageFamilyName = "fc793027-d487-40c0-899e-66562fd62f8c_nr8wym1fh6evt";

        string boundaryName = Guid.NewGuid().ToString();
        using var boundary = new BoundaryDescriptor(boundaryName, CreateFlags.AddAppContainerSID);
        boundary.Add(WellKnownSidType.WorldSid);
        boundary.Add(WellKnownSidType.WinLowLabelSid);
        Debug.WriteLine($"boundary: {boundaryName}");

        var security = new DirectorySecurity();
        security.AddAccessRule(new(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl,
                                   AccessControlType.Allow));
        UserEnv.DeriveAppContainerSidFromAppContainerName(packageFamilyName,
                                                          out var containerSID).ThrowIfFailed();

        string sid = AdvApi32.ConvertSidToStringSid(containerSID);
        security.AddAccessRule(new(new SecurityIdentifier(sid),
                                   FileSystemRights.FullControl,
                                   AccessControlType.Allow));

        string name = Guid.NewGuid().ToString();
        using var ns = new PrivateNamespace(security, boundary, name, destroyOnClose: true);

        string semaphoreName = $"{name}\\{Guid.NewGuid()}";
        using var semaphore = new Semaphore(initialCount: 0, maximumCount: 1, semaphoreName);
        Debug.WriteLine(semaphoreName);

        Assert.True(semaphore.WaitOne(TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public async Task ShareSecured() {
        string boundaryName = Guid.NewGuid().ToString();
        using var boundary = new BoundaryDescriptor(boundaryName, CreateFlags.AddAppContainerSID);
        boundary.Add(WellKnownSidType.WorldSid);
        Debug.WriteLine($"boundary: {boundaryName}");

        var security = new DirectorySecurity();
        security.AddAccessRule(new(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl,
                                   AccessControlType.Allow));

        string name = Guid.NewGuid().ToString();
        using var ns = new PrivateNamespace(security, boundary, name, destroyOnClose: true);

        string semaphoreName = $"{name}\\{Guid.NewGuid()}";

        await this.TestSemaphoreShare(boundaryName: boundaryName,
                                      semaphoreName: semaphoreName);
    }

    [Fact]
    public async Task Share() {
        string boundaryName = Guid.NewGuid().ToString();
        using var boundary = new BoundaryDescriptor(boundaryName, CreateFlags.AddAppContainerSID);
        boundary.Add(WellKnownSidType.WorldSid);
        Debug.WriteLine($"boundary: {boundaryName}");

        string name = Guid.NewGuid().ToString();
        using var ns = new PrivateNamespace(boundary, name, destroyOnClose: true);

        string semaphoreName = $"{name}\\{Guid.NewGuid()}";

        await this.TestSemaphoreShare(boundaryName: boundaryName,
                                      semaphoreName: semaphoreName);
    }

    async Task TestSemaphoreShare(string boundaryName, string semaphoreName) {
        using var semaphore = new Semaphore(initialCount: 0, maximumCount: 1, semaphoreName);
        Debug.WriteLine(semaphoreName);

        Assert.False(semaphore.WaitOne(0)); // sanity

        var startInfo = new ProcessStartInfo(OutsideProcess.GetExePath()) {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            Environment = {
                ["BOUNDARY"] = boundaryName,
                ["SEMAPHORE"] = semaphoreName,
            },
        };

        using var child = Process.Start(startInfo)!;
        string error = await child.StandardError.ReadToEndAsync();
        await child.WaitForExitAsync();
        Assert.True(child.ExitCode == 0, error);

        Assert.True(semaphore.WaitOne(0));
    }
}