using System.Text;
using System.IO.Compression;
using LightSpace.Imaging;
namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    public async Task ImportAsync()
    {
        var files = await _storage.OpenImagesAsync();
        if (files.Count == 0) return;
        var imported = 0; var errors = new List<string>();
        var total = Session.Catalog.Photos.Sum(p => (long)p.Original.Length);
        foreach (var file in files)
        {
            try
            {
                if (total + file.Bytes.Length > CatalogSerializer.MaxCatalogBytes)
                    throw new InvalidDataException("Catalog originals exceed the 256 MiB limit. Start another catalog.");
                if (Session.Catalog.Photos.Count >= 5000)
                    throw new InvalidDataException("The catalog has reached its 5,000-photo safety limit.");
                var photo = PhotoCodec.Import(file.Name, file.Bytes);
                Session.Add(photo); total += file.Bytes.Length; imported++;
            }
            catch (Exception error) { errors.Add(file.Name + ": " + error.Message); }
        }
        SetGrid(false); ChooseTool(PhotoTool.Edit);
        SetStatus($"Imported {imported} photo{(imported == 1 ? "" : "s")}." +
            (errors.Count > 0 ? " " + string.Join(" ", errors) : " Originals remain unchanged."));
    }
    public async Task OpenCatalogAsync()
    {
        var file = await _storage.OpenCatalogAsync(); if (file is null) return;
        if (file.Bytes.Length > CatalogSerializer.MaxCatalogBytes * 1.4)
            throw new InvalidDataException("Catalog file exceeds the size limit.");
        var catalog = CatalogSerializer.Deserialize(Encoding.UTF8.GetString(file.Bytes));
        _renderer.Clear(); _thumbnails.Clear(); Session.Load(catalog);
        _query = new(); _page = 0; _search.Text = ""; RefreshAll(); SetStatus("Opened " + file.Name);
    }
    public async Task SaveCatalogAsync()
    {
        Session.CommitGesture("Adjustment");
        await _storage.SaveAsync("LightSpace-catalog.lightspace",
            Encoding.UTF8.GetBytes(CatalogSerializer.Serialize(Session.Catalog)), "application/json");
        SetStatus("Catalog backup exported, including original photographs and edit settings.");
    }
    private Task CreateAlbumAsync() => TextPromptAsync("New album", "Album name", "Untitled album", value =>
    {
        Session.CreateAlbum(value); SetStatus("Album created from the current selection.");
    });
    private Task SaveVersionAsync() => TextPromptAsync("Create version", "Version name",
        "Version " + ((Session.Active?.Versions.Count ?? 0) + 1), Session.SaveVersion);
    private Task ShowExportAsync()
    {
        if (Session.Active is null) return Task.CompletedTask;
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(Note("Export a rendered copy. Your original remains in the catalog. Output is 8-bit sRGB; source metadata is not copied."));
        var format = new ComboBox
        {
            ItemsSource = new[] { "JPEG", "PNG", "WebP" }, SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch, FontFamily = Theme.Font
        };
        AutomationProperties.SetName(format, "Export format");
        panel.Children.Add(Theme.Text("File type", 11, true)); panel.Children.Add(format);
        var quality = new AdjustmentSlider("Quality", 1, 100, 92) { Value = 92 }; panel.Children.Add(quality);
        var size = new ComboBox
        {
            ItemsSource = new[] { "Full size (up to 8192 px)", "4096 px", "2048 px", "1280 px" },
            SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch, FontFamily = Theme.Font
        };
        AutomationProperties.SetName(size, "Export size");
        panel.Children.Add(Theme.Text("Long edge", 11, true)); panel.Children.Add(size);
        var selection = new CheckBox { Content = "Export selected photos as a ZIP", IsChecked = false, FontFamily = Theme.Font };
        panel.Children.Add(selection);
        ShowDialog("Export photographs", panel, "Export file", async () =>
        {
            var encoded = format.SelectedIndex switch { 1 => SKEncodedImageFormat.Png, 2 => SKEncodedImageFormat.Webp, _ => SKEncodedImageFormat.Jpeg };
            var extension = format.SelectedIndex switch { 1 => "png", 2 => "webp", _ => "jpg" };
            var mime = "image/" + (extension == "jpg" ? "jpeg" : extension);
            var limit = size.SelectedIndex switch { 1 => 4096, 2 => 2048, 3 => 1280, _ => 0 };
            Session.CommitGesture("Adjustment");
            if (selection.IsChecked == true && Session.Selection.Count > 1)
            {
                using var stream = new MemoryStream();
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    var index = 0;
                    foreach (var photo in Session.Catalog.Photos.Where(p => Session.Selection.Contains(p.Id)))
                    {
                        var bytes = _renderer.Export(photo, encoded, (int)quality.Value, limit);
                        var entry = zip.CreateEntry($"{++index:000}-{SafeName(photo.Name)}.{extension}", CompressionLevel.NoCompression);
                        using var output = entry.Open(); output.Write(bytes);
                    }
                }
                await _storage.SaveAsync("LightSpace-export.zip", stream.ToArray(), "application/zip");
            }
            else if (Session.Active is { } photo)
                await _storage.SaveAsync(SafeName(photo.Name) + "-edited." + extension,
                    _renderer.Export(photo, encoded, (int)quality.Value, limit), mime);
            SetStatus("Export complete. Original retained in the catalog.");
        });
        return Task.CompletedTask;
    }
    private static string SafeName(string name)
    {
        name = Path.GetFileNameWithoutExtension(name);
        var clean = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ' ').Take(100).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "photo" : clean;
    }
    private void Navigate(int delta)
    {
        if (_visible.Count == 0) return;
        var index = _visible.ToList().FindIndex(p => p.Id == Session.Catalog.ActivePhoto);
        index = Math.Clamp(index + delta, 0, _visible.Count - 1);
        _page = index / PageSize; Session.Select(_visible[index].Id); RefreshCatalog();
    }
}
