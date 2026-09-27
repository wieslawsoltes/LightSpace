using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Controls;
using LightSpace.Editing;
using LightSpace.Imaging;
using LightSpace.Storage;
using LightSpace.Workbench;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace LightSpace.App;

public sealed partial class App : Application
{
    private Window? _window;
    private StudioView? _studio;
    public App(){InitializeComponent();RequestedTheme=ApplicationTheme.Dark;}
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window=new Window{Title="LightSpace"};
        _window.Content=new Grid{Background=Theme.Background,Children={new TextBlock{Text="LightSpace\nOpening your photography workspace…",FontSize=20,TextAlignment=TextAlignment.Center,Foreground=Theme.TextColor,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}}};_window.Activate();
        try
        {
            Theme.Font=new FontFamily("ms-appx:///Assets/Fonts/NotoSans.ttf#Noto Sans");
#if __WASM__
            IWorkspaceStorage storage=new BrowserWorkspaceStorage();
#else
            IWorkspaceStorage storage=new DesktopWorkspaceStorage();
#endif
            CatalogDocument? catalog=null;string? warning=null;
            try{var recovery=await storage.ReadRecoveryAsync();if(!string.IsNullOrWhiteSpace(recovery))catalog=CatalogSerializer.Deserialize(recovery);}catch(Exception e){warning="Recovery could not be opened; stored data has not been deleted. "+e.Message;}
            var recovered = catalog is not null;
            catalog??=LoadSamples();
            _studio=new StudioView(new EditorSession(catalog),storage,recovered);
#if __WASM__
            _studio.UnsavedChangesChanged += BrowserFiles.SetDirty;
            BrowserFiles.SetDirty(_studio.Recovery.HasUnsavedChanges);
            // Normal sessions do not walk the arranged UI tree or serialize
            // diagnostic snapshots on a periodic timer.
            if (BrowserDiagnostics.Enabled())
                _studio.DiagnosticsChanged+=diagnostics=>BrowserFiles.PublishDiagnostics(JsonSerializer.Serialize(diagnostics,AppJsonContext.Default.StudioDiagnostics));
            _studio.GotFocus+=(_,_)=>BrowserFiles.FocusCanvasUnlessEditing();
#endif
            _window.Content=_studio;_window.Closed+=(_,_)=>_studio.Dispose();if(warning is not null)_studio.ProtectExistingRecovery(warning);
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e);_window.Content=new ScrollViewer{Content=new TextBlock{Text="LightSpace could not start\n\n"+e,TextWrapping=TextWrapping.Wrap,Foreground=Theme.TextColor,Margin=new Thickness(30),FontSize=14}};
#if __WASM__
            BrowserFiles.StartupError(e.ToString());
#endif
        }
    }
    private static CatalogDocument LoadSamples()
    {
        var assembly=typeof(App).Assembly;var names=assembly.GetManifestResourceNames().Where(n=>n.Contains(".Assets.Samples.")&&n.EndsWith(".jpg",StringComparison.Ordinal)).Order().ToArray();
        if(names.Length==0)return SamplePhotos.CreateCatalog();
        var catalog=new CatalogDocument();var i=0;
        foreach(var name in names)
        {
            using var input=assembly.GetManifestResourceStream(name)!;using var memory=new MemoryStream();input.CopyTo(memory);
            var title=name[(name.IndexOf(".Assets.Samples.",StringComparison.Ordinal)+16)..].Replace('-', ' ');
            var photo=PhotoCodec.Import(title,memory.ToArray());photo.ExposureInfo="Bundled demonstration photograph · Unsplash";photo.State=new(){Rating=i==0?5:i%4,Flag=i%3==0?PhotoFlag.Pick:PhotoFlag.None,Keywords=["landscape","sample"]};catalog.Photos.Add(photo);i++;
        }
        catalog.ActivePhoto=catalog.Photos[0].Id;catalog.Albums.Add(new(){Name="Alpine stories",Photos=catalog.Photos.Take(4).Select(p=>p.Id).ToList()});catalog.Albums.Add(new(){Name="Quiet moments",Photos=catalog.Photos.Skip(3).Select(p=>p.Id).ToList()});return catalog;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy=JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StudioDiagnostics))]
internal partial class AppJsonContext : JsonSerializerContext;
