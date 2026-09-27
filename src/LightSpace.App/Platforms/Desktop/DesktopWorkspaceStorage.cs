using LightSpace.Storage;
using Windows.Storage;
using Windows.Storage.Pickers;
namespace LightSpace.App;

internal sealed class DesktopWorkspaceStorage : IWorkspaceStorage
{
    private static string DirectoryPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LightSpace");
    private static string RecoveryPath=>Path.Combine(DirectoryPath,"recovery.lightspace");
    public async Task<IReadOnlyList<WorkspaceFile>> OpenImagesAsync()
    {
        var picker=new FileOpenPicker{SuggestedStartLocation=PickerLocationId.PicturesLibrary};foreach(var extension in new[]{".jpg",".jpeg",".png",".webp",".bmp",".gif"})picker.FileTypeFilter.Add(extension);
        var files=await picker.PickMultipleFilesAsync();var result=new List<WorkspaceFile>();long total=0;
        foreach(var file in files){var loaded=await Read(file,64*1024*1024);total+=loaded.Bytes.Length;if(total>256*1024*1024)throw new InvalidDataException("An import batch cannot exceed 256 MiB.");result.Add(loaded);}return result;
    }
    public async Task<WorkspaceFile?> OpenCatalogAsync()
    {
        var picker=new FileOpenPicker{SuggestedStartLocation=PickerLocationId.DocumentsLibrary};picker.FileTypeFilter.Add(".lightspace");picker.FileTypeFilter.Add(".json");var file=await picker.PickSingleFileAsync();return file is null?null:await Read(file,360*1024*1024);
    }
    private static async Task<WorkspaceFile> Read(StorageFile file,int limit)
    {
        using var input=await file.OpenStreamForReadAsync();if(input.Length>limit)throw new InvalidDataException("The selected file exceeds the size limit.");using var output=new MemoryStream();await input.CopyToAsync(output);if(output.Length>limit)throw new InvalidDataException("The file grew beyond its size limit.");return new(file.Name,output.ToArray());
    }
    public async Task SaveAsync(string name,byte[] bytes,string mimeType)
    {
        var picker=new FileSavePicker{SuggestedFileName=Path.GetFileNameWithoutExtension(name),SuggestedStartLocation=PickerLocationId.PicturesLibrary};picker.FileTypeChoices.Add("LightSpace export",new List<string>{Path.GetExtension(name)});var file=await picker.PickSaveFileAsync();if(file is null)throw new OperationCanceledException("Save canceled.");await FileIO.WriteBytesAsync(file,bytes);
    }
    public async Task<string?> ReadRecoveryAsync()
    {
        if(!File.Exists(RecoveryPath))return null;if(new FileInfo(RecoveryPath).Length>360L*1024*1024)throw new InvalidDataException("Recovery exceeds the safety limit.");return await File.ReadAllTextAsync(RecoveryPath);
    }
    public async Task WriteRecoveryAsync(string catalog)
    {
        Directory.CreateDirectory(DirectoryPath);var temporary=RecoveryPath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{await File.WriteAllTextAsync(temporary,catalog);File.Move(temporary,RecoveryPath,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
