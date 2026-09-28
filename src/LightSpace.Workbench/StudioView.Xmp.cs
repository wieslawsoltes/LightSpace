using System.Text;
namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private async Task ImportXmpAsync()
    {
        if (Session.Active is not { } photo) return;
        if (_storage is not ISidecarStorage picker) throw new NotSupportedException("This host does not provide an XMP sidecar picker.");
        var id = photo.Id; var catalog = Session.Catalog;
        var file = await picker.OpenSidecarAsync(); if (file is null) return;
        if (file.Bytes.Length > XmpSidecar.MaximumBytes) throw new InvalidDataException("The XMP sidecar exceeds the 16 MiB limit.");
        var xml = new UTF8Encoding(false, true).GetString(file.Bytes).TrimStart('\uFEFF');
        if (!ReferenceEquals(catalog, Session.Catalog) || Session.Active?.Id != id) throw new InvalidOperationException("The selected photograph changed while choosing the sidecar. Import again for the intended photograph.");
        Session.CommitGesture("Adjustment");
        var baseline = photo.State;
        var metadataOnly = new CheckBox { Content = "Import metadata only", FontFamily = Theme.Font };
        var preferNative = new CheckBox { Content = "Prefer embedded LightSpace settings", IsChecked = true, FontFamily = Theme.Font };
        AutomationProperties.SetName(metadataOnly, "XMP metadata only"); AutomationProperties.SetName(preferNative, "XMP prefer native");
        Register("XMP metadata only", metadataOnly); Register("XMP prefer native", preferNative);
        var report = Note(""); Register("XMP import report", report);
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(Theme.Text(file.Name, 13)); panel.Children.Add(metadataOnly); panel.Children.Add(preferNative); panel.Children.Add(report);
        XmpImportResult? parsed = null;
        void Review()
        {
            try
            {
                parsed = XmpSidecar.Import(xml, baseline, new(metadataOnly.IsChecked == true, preferNative.IsChecked == true));
                report.Text = "Applied fields: " + string.Join(", ", parsed.AppliedProperties) + "\n\n" + string.Join("\n", parsed.Warnings);
                if (parsed.AppliedProperties.Length == 0) report.Text = "No supported fields will be changed.\n\n" + string.Join("\n", parsed.Warnings);
            }
            catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException)
            {
                parsed = null; report.Text = "Cannot import: " + error.Message;
            }
        }
        metadataOnly.Checked += (_, _) => Review(); metadataOnly.Unchecked += (_, _) => Review();
        preferNative.Checked += (_, _) => Review(); preferNative.Unchecked += (_, _) => Review(); Review();
        ShowDialog("Import XMP sidecar", panel, "Apply XMP", () =>
        {
            if (parsed is null) throw new InvalidDataException("Resolve the sidecar errors before importing.");
            if (!ReferenceEquals(catalog, Session.Catalog) || Session.Active?.Id != id || !PhotoStateEquality.All(photo.State, baseline))
                throw new InvalidOperationException("The photograph changed during review. Close this dialog and import again.");
            Session.Edit("Import XMP: " + file.Name, _ => parsed.State);
            BuildInspector(); SetStatus($"XMP imported: {parsed.AppliedProperties.Length} fields, {parsed.Warnings.Length} compatibility notices.");
            return Task.CompletedTask;
        });
    }
    private Task ExportXmpAsync()
    {
        if (Session.Active is not { } photo) return Task.CompletedTask;
        Session.CommitGesture("Adjustment"); var state = photo.State;
        var includeDevelopment = new CheckBox { Content = "Include supported development values", IsChecked = true, FontFamily = Theme.Font };
        var includeNative = new CheckBox { Content = "Include complete LightSpace settings", IsChecked = true, FontFamily = Theme.Font };
        AutomationProperties.SetName(includeDevelopment, "XMP include development"); AutomationProperties.SetName(includeNative, "XMP include native");
        Register("XMP include development", includeDevelopment); Register("XMP include native", includeNative);
        var report = Note(""); var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(Note("Creates a separate XMP sidecar; the original image is not modified. Standard metadata includes rating, label, caption and keywords. Adobe rendering equivalence is not guaranteed."));
        panel.Children.Add(includeDevelopment); panel.Children.Add(includeNative); panel.Children.Add(report);
        XmpExportResult? result = null;
        void Review()
        {
            includeNative.IsEnabled = includeDevelopment.IsChecked == true;
            try
            {
                result = XmpSidecar.Export(state, includeDevelopment.IsChecked == true, includeNative.IsChecked == true);
                report.Text = string.Join("\n", result.Warnings);
            }
            catch (InvalidDataException error) { result = null; report.Text = error.Message; }
        }
        includeDevelopment.Checked += (_, _) => Review(); includeDevelopment.Unchecked += (_, _) => Review();
        includeNative.Checked += (_, _) => Review(); includeNative.Unchecked += (_, _) => Review(); Review();
        ShowDialog("Export XMP sidecar", panel, "Save XMP", async () =>
        {
            if (result is null) throw new InvalidDataException("The sidecar could not be created. Export a catalog backup instead.");
            await _storage.SaveAsync(SafeName(photo.Name) + ".xmp", Encoding.UTF8.GetBytes(result.Xml), "application/rdf+xml");
            SetStatus("XMP sidecar exported. Original image unchanged.");
        });
        return Task.CompletedTask;
    }
}
