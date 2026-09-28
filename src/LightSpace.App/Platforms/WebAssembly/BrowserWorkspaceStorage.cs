using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using LightSpace.Storage;
namespace LightSpace.App;

internal sealed class BrowserWorkspaceStorage : IWorkspaceStorage
{
    public async Task<IReadOnlyList<WorkspaceFile>> OpenImagesAsync()=>Decode(await BrowserFiles.OpenImages());
    public async Task<WorkspaceFile?> OpenCatalogAsync()=>Decode(await BrowserFiles.OpenCatalog()).FirstOrDefault();
    private static IReadOnlyList<WorkspaceFile> Decode(string json)
    {
        if(string.IsNullOrWhiteSpace(json))return [];using var document=JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().Select(file=>new WorkspaceFile(file.GetProperty("name").GetString()??"photo",Convert.FromBase64String(file.GetProperty("base64").GetString()??""))).ToArray();
    }
    public async Task SaveAsync(string name,byte[] bytes,string mimeType)=>await BrowserFiles.Download(name,Convert.ToBase64String(bytes),mimeType);
    public async Task<string?> ReadRecoveryAsync()=>await BrowserFiles.Load();
    public async Task WriteRecoveryAsync(string catalog)=>await BrowserFiles.Save(catalog);
}
internal static partial class BrowserFiles
{
    [JSImport("globalThis.lightSpaceFiles.openImages")][return:JSMarshalAs<JSType.Promise<JSType.String>>]internal static partial Task<string> OpenImages();
    [JSImport("globalThis.lightSpaceFiles.openCatalog")][return:JSMarshalAs<JSType.Promise<JSType.String>>]internal static partial Task<string> OpenCatalog();
    [JSImport("globalThis.lightSpaceFiles.download")][return:JSMarshalAs<JSType.Promise<JSType.String>>]internal static partial Task<string> Download(string name,string base64,string type);
    [JSImport("globalThis.lightSpaceFiles.load")][return:JSMarshalAs<JSType.Promise<JSType.String>>]internal static partial Task<string> Load();
    [JSImport("globalThis.lightSpaceFiles.save")][return:JSMarshalAs<JSType.Promise<JSType.String>>]internal static partial Task<string> Save(string json);
    [JSImport("globalThis.lightSpaceFiles.publishDiagnostics")]internal static partial void PublishDiagnostics(string json);
    [JSImport("globalThis.lightSpaceFiles.focusCanvasUnlessEditing")]internal static partial void FocusCanvasUnlessEditing();
    [JSImport("globalThis.lightSpaceFiles.setDirty")]internal static partial void SetDirty(bool dirty);
    [JSImport("globalThis.lightSpaceFiles.startupError")]internal static partial void StartupError(string error);
}
