using System.Text;
using System.IO.Compression;
using LightSpace.Imaging;
namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private Border? _dialogOverlay;
    public async Task ImportAsync()
    {
        var files=await _storage.OpenImagesAsync();if(files.Count==0)return;
        var imported=0;var errors=new List<string>();var total=Session.Catalog.Photos.Sum(p=>(long)p.Original.Length);
        foreach(var file in files)
        {
            try
            {
                if(total+file.Bytes.Length>CatalogSerializer.MaxCatalogBytes)throw new InvalidDataException("Catalog originals exceed the 256 MiB limit. Start another catalog.");
                var photo=PhotoCodec.Import(file.Name,file.Bytes);Session.Add(photo);total+=file.Bytes.Length;imported++;
            }
            catch(Exception e){errors.Add(file.Name+": "+e.Message);}
        }
        SetGrid(false);ChooseTool(PhotoTool.Edit);SetStatus($"Imported {imported} photo{(imported==1?"":"s")}."+(errors.Count>0?" "+string.Join(" ",errors):" Originals remain unchanged."));
    }
    public async Task OpenCatalogAsync()
    {
        var file=await _storage.OpenCatalogAsync();if(file is null)return;
        if(file.Bytes.Length>CatalogSerializer.MaxCatalogBytes*1.4)throw new InvalidDataException("Catalog file exceeds the size limit.");
        var catalog=CatalogSerializer.Deserialize(Encoding.UTF8.GetString(file.Bytes));
        _renderer.Clear();_thumbnails.Clear();Session.Load(catalog);_query=new();_page=0;_search.Text="";RefreshAll();SetStatus("Opened "+file.Name);
    }
    public async Task SaveCatalogAsync()
    {
        Session.CommitGesture("Adjustment");await _storage.SaveAsync("LightSpace-catalog.lightspace",Encoding.UTF8.GetBytes(CatalogSerializer.Serialize(Session.Catalog)),"application/json");SetStatus("Catalog backup exported, including original photographs and edit settings.");
    }
    private Task CreateAlbumAsync()=>TextPromptAsync("New album","Album name","Untitled album",value=>{Session.CreateAlbum(value);SetStatus("Album created from the current selection.");});
    private Task SaveVersionAsync()=>TextPromptAsync("Create version","Version name","Version "+((Session.Active?.Versions.Count??0)+1),Session.SaveVersion);
    private Task TextPromptAsync(string title,string placeholder,string value,Action<string> apply)
    {
        var input=Theme.Input(placeholder,placeholder,value);var panel=new StackPanel{Spacing=12};panel.Children.Add(input);
        ShowDialog(title,panel,"Create",()=>{if(string.IsNullOrWhiteSpace(input.Text))throw new InvalidOperationException("A name is required.");apply(input.Text);return Task.CompletedTask;});return Task.CompletedTask;
    }
    private Task ShowExportAsync()
    {
        if(Session.Active is null)return Task.CompletedTask;
        var panel=new StackPanel{Spacing=12};panel.Children.Add(Note("Export a rendered copy. Your original remains in the catalog. Output is 8-bit sRGB; source metadata is not copied."));
        var format=new ComboBox{ItemsSource=new[]{"JPEG","PNG","WebP"},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch,FontFamily=Theme.Font};AutomationProperties.SetName(format,"Export format");panel.Children.Add(Theme.Text("File type",11,true));panel.Children.Add(format);
        var quality=new AdjustmentSlider("Quality",1,100,92){Value=92};panel.Children.Add(quality);
        var size=new ComboBox{ItemsSource=new[]{"Full size (up to 8192 px)","4096 px","2048 px","1280 px"},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch,FontFamily=Theme.Font};AutomationProperties.SetName(size,"Export size");panel.Children.Add(Theme.Text("Long edge",11,true));panel.Children.Add(size);
        var selection=new CheckBox{Content="Export selected photos as a ZIP",IsChecked=false,FontFamily=Theme.Font};panel.Children.Add(selection);
        ShowDialog("Export photographs",panel,"Export file",async()=>
        {
            var encoded=format.SelectedIndex switch{1=>SKEncodedImageFormat.Png,2=>SKEncodedImageFormat.Webp,_=>SKEncodedImageFormat.Jpeg};var extension=format.SelectedIndex switch{1=>"png",2=>"webp",_=>"jpg"};var mime="image/"+(extension=="jpg"?"jpeg":extension);var limit=size.SelectedIndex switch{1=>4096,2=>2048,3=>1280,_=>0};
            Session.CommitGesture("Adjustment");
            if(selection.IsChecked==true&&Session.Selection.Count>1)
            {
                using var stream=new MemoryStream();using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true))
                {
                    var index=0;foreach(var photo in Session.Catalog.Photos.Where(p=>Session.Selection.Contains(p.Id)))
                    {
                        var bytes=_renderer.Export(photo,encoded,(int)quality.Value,limit);var entry=zip.CreateEntry($"{++index:000}-{SafeName(photo.Name)}.{extension}",CompressionLevel.NoCompression);using var output=entry.Open();output.Write(bytes);
                    }
                }
                await _storage.SaveAsync("LightSpace-export.zip",stream.ToArray(),"application/zip");
            }
            else if(Session.Active is{} photo)await _storage.SaveAsync(SafeName(photo.Name)+"-edited."+extension,_renderer.Export(photo,encoded,(int)quality.Value,limit),mime);
            SetStatus("Export complete. Original retained in the catalog.");
        });return Task.CompletedTask;
    }
    private static string SafeName(string name)
    {
        name=Path.GetFileNameWithoutExtension(name);var clean=new string(name.Where(c=>char.IsLetterOrDigit(c)||c is '-' or '_' or ' ').Take(100).ToArray());return string.IsNullOrWhiteSpace(clean)?"photo":clean;
    }
    private Task ShowHelpAsync()
    {
        var panel=new StackPanel{Spacing=10};
        foreach(var (title,body) in new[]{
            ("Start with your photographs","Add photos imports JPEG, PNG, WebP, BMP and GIF. The supplied landscapes are demonstration images. Camera RAW formats are not decoded."),
            ("Develop without destroying","Sliders, point curves, color mixing, crop, gradients and clone spots retain the original bytes. Double-click a slider to reset it; numeric values can be typed directly. Undo keeps up to 100 transactions."),
            ("Select and organize","Use the grid or filmstrip. Control-click selects several photos. Ratings, flags, keywords and captions support filtering. Albums reference photographs rather than making duplicate originals."),
            ("Keyboard","G: grid · E/D: edit · R: crop · M: mask · Z: zoom · Y: compare · \\: original · 0–5: rating · P: pick · X: reject · U: clear flag · arrows: previous/next · Ctrl/Cmd+Z: undo · Ctrl/Cmd+Shift+Z: redo · Ctrl/Cmd+I: import · Ctrl/Cmd+S: catalog backup."),
            ("Crop, mask and clone","Drag crop handles or a new crop rectangle; Done returns to editing. Drag on the photo to create a radial or vertical linear mask. Alt-click a source then click a destination with Clone. Masks are limited to eight, clone spots to 32."),
            ("Local storage and export","Recovery is written to this browser's IndexedDB or the desktop application-data directory. It is not encrypted or cloud-backed. Export a .lightspace catalog backup before clearing browser storage. JPEG/PNG/WebP export is 8-bit sRGB with an 8192-pixel long-edge safety cap."),
            ("About this release","LightSpace is independent MIT-licensed software, not an Adobe product. It is a functional photo workspace, not complete Lightroom parity. No Adobe camera profiles, RAW pipeline, AI denoise/removal, lens database, HDR/panorama merge, cloud synchronization or Lightroom catalog compatibility are included.")})
        {panel.Children.Add(Theme.Text(title,13));panel.Children.Add(Note(body));}
        ShowDialog("LightSpace guide",panel,"Close",()=>Task.CompletedTask);return Task.CompletedTask;
    }
    private void ShowDialog(string title,FrameworkElement body,string confirm,Func<Task> apply)
    {
        CloseDialog();if(Content is not Grid root)return;
        var sheet=new Grid{MaxWidth=520,MaxHeight=Math.Max(300,ActualHeight-60),MinWidth=340,Background=Theme.Panel,Padding=new(24),CornerRadius=new(8),RowDefinitions={new(){Height=GridLength.Auto},new(){Height=new(1,GridUnitType.Star)},new(){Height=GridLength.Auto}}};
        var heading=Theme.Text(title,21);heading.Margin=new(0,0,0,18);sheet.Children.Add(heading);
        var scroll=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetRow(scroll,1);sheet.Children.Add(scroll);
        var actions=Row();actions.HorizontalAlignment=HorizontalAlignment.Right;actions.Margin=new(0,18,0,0);actions.Children.Add(Button("Cancel dialog",Glyph.None,"Cancel",CloseDialog));
        var button=Button(confirm,Glyph.None,confirm);button.Selected=true;button.Click+=async(_,_)=>{button.IsEnabled=false;try{await apply();CloseDialog();}catch(Exception e){SetStatus(e.Message);button.IsEnabled=true;}};actions.Children.Add(button);Grid.SetRow(actions,2);sheet.Children.Add(actions);
        _dialogOverlay=new Border{Background=Theme.Brush("#aa000000"),Child=sheet,HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Stretch};Grid.SetRowSpan(_dialogOverlay,2);root.Children.Add(_dialogOverlay);PublishDiagnostics();
    }
    private void CloseDialog(){if(_dialogOverlay is null)return;if(Content is Grid root)root.Children.Remove(_dialogOverlay);_dialogOverlay=null;PublishDiagnostics();}
    private void Keyboard(object sender,KeyRoutedEventArgs e)
    {
        if(XamlRoot is { } root && FocusManager.GetFocusedElement(root) is TextBox)return;
        if(_dialogOverlay is not null){if(e.Key==VirtualKey.Escape){CloseDialog();e.Handled=true;}return;}
        bool Down(VirtualKey key)=>Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var control=Down(VirtualKey.Control)||Down(VirtualKey.LeftWindows)||Down(VirtualKey.RightWindows);var shift=Down(VirtualKey.Shift);var handled=true;
        if(control)
        {
            switch(e.Key)
            {
                case VirtualKey.Z:if(shift)Session.Redo();else Session.Undo();break;
                case VirtualKey.Y:Session.Redo();break;
                case VirtualKey.I:Run(ImportAsync);break;
                case VirtualKey.S:Run(SaveCatalogAsync);break;
                case VirtualKey.E:Run(ShowExportAsync);break;
                case VirtualKey.A:Session.Selection.UnionWith(_visible.Select(p=>p.Id));RefreshCatalog();break;
                case VirtualKey.C:_clipboard=Session.Active?.State;SetStatus("Edit settings copied.");break;
                case VirtualKey.V:PasteSettings();break;
                default:handled=false;break;
            }
        }
        else if(e.Key>=VirtualKey.Number0&&e.Key<=VirtualKey.Number5){var rating=(int)e.Key-(int)VirtualKey.Number0;Session.Edit("Rating",s=>s with{Rating=rating},true);}
        else switch(e.Key)
        {
            case VirtualKey.G:SetGrid(true);break;
            case VirtualKey.E:case VirtualKey.D:ChooseTool(PhotoTool.Edit);break;
            case VirtualKey.R:ChooseTool(PhotoTool.Crop);break;
            case VirtualKey.M:ChooseTool(PhotoTool.RadialMask);break;
            case VirtualKey.Z:Viewport.ToggleZoom();break;
            case VirtualKey.Y:Viewport.Compare=!Viewport.Compare;Viewport.Invalidate();break;
            case VirtualKey.P:Session.Edit("Pick",s=>s with{Flag=PhotoFlag.Pick},true);break;
            case VirtualKey.X:Session.Edit("Reject",s=>s with{Flag=PhotoFlag.Reject},true);break;
            case VirtualKey.U:Session.Edit("Clear flag",s=>s with{Flag=PhotoFlag.None},true);break;
            case VirtualKey.Left:Navigate(-1);break;
            case VirtualKey.Right:Navigate(1);break;
            case VirtualKey.Escape:ChooseTool(PhotoTool.Edit);break;
            default:if((int)e.Key==220){Viewport.Before=!Viewport.Before;Viewport.Invalidate();}else handled=false;break;
        }
        e.Handled=handled;
    }
    private void Navigate(int delta)
    {
        if(_visible.Count==0)return;var index=_visible.ToList().FindIndex(p=>p.Id==Session.Catalog.ActivePhoto);index=Math.Clamp(index+delta,0,_visible.Count-1);_page=index/PageSize;Session.Select(_visible[index].Id);RefreshCatalog();
    }
}
