using System.Runtime.InteropServices.JavaScript;
namespace LightSpace.App;

internal static partial class BrowserDiagnostics
{
    [JSImport("globalThis.lightSpaceDiagnosticsEnabled")]
    internal static partial bool Enabled();
}
