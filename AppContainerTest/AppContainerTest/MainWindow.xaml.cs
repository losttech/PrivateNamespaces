namespace AppContainerTest;

using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

using LostTech.Win32.PrivateNamespaces;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel;

public sealed partial class MainWindow: Window {
    public MainWindow() {
        this.InitializeComponent();

        this.Family.Text = Package.Current.Id.FamilyName;
    }

    void Button_Click(object sender, RoutedEventArgs e) {
        try {
            string[] parts = this.Path.Text.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            Debug.Assert(parts.Length == 2);

            using var boundary = new BoundaryDescriptor(
                this.BoundaryName.Text,
                BoundaryDescriptor.CreateFlags.AddAppContainerSID);
            boundary.Add(WellKnownSidType.WorldSid);
            boundary.Add(WellKnownSidType.WinLowLabelSid);

            using var ns = PrivateNamespace.Open(boundary, parts[0]);
            try {
                using var semaphore = Semaphore.OpenExisting(this.Path.Text);
                semaphore.Release();
                this.Error.Text = "";
            } catch (Exception ex) {
                this.Error.Text = $"unable to operate semaphore: {ex}";
            }
        } catch (Exception ex) {
            this.Error.Text = $"unable to open private namespace: {ex}";
        }
    }
}