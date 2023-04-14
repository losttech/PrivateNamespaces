namespace LostTech.Win32.PrivateNamespaces;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

public static class OutsideProcess {
    static void Main() {
        string boundaryName = Environment.GetEnvironmentVariable("BOUNDARY")!;
        string semaphoreName = Environment.GetEnvironmentVariable("SEMAPHORE")!;

        string[] parts = semaphoreName.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        Debug.Assert(parts.Length == 2);
        string namespaceName = parts[0];

        using var boundary = new BoundaryDescriptor(boundaryName,
                                                    BoundaryDescriptor.CreateFlags.None);
        boundary.Add(WellKnownSidType.WorldSid);
        using var ns = PrivateNamespace.Open(boundary, namespaceName);
        using var semaphore = Semaphore.OpenExisting(semaphoreName);
        semaphore.Release();
    }

    public static string GetExePath() {
        string dll = Assembly.GetExecutingAssembly().Location;
        return Path.ChangeExtension(dll, ".exe");
    }
}