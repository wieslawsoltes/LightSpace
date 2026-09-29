using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LightSpace.Core;
namespace LightSpace.Catalog;

public sealed record XmpImportOptions(bool MetadataOnly = false, bool PreferLightSpaceSettings = true);
public sealed record XmpImportResult(PhotoState State, string[] AppliedProperties, string[] Warnings, bool UsedNativeSettings);
public sealed record XmpExportResult(string Xml, string[] Warnings);

/// <summary>Bounded XMP/RDF sidecars with explicit metadata and Camera Raw parameter mappings.</summary>
public static partial class XmpSidecar
{
    public const int MaximumBytes = 16 * 1024 * 1024;
    public const int NativeSchemaVersion = 5;
    public static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    public static readonly XNamespace Xmp = "http://ns.adobe.com/xap/1.0/";
    public static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    public static readonly XNamespace Crs = "http://ns.adobe.com/camera-raw-settings/1.0/";
    public static readonly XNamespace Native = "https://wieslawsoltes.github.io/LightSpace/ns/1.0/";
    private static readonly (string Property, string Setting)[] NumericProperties =
    [
        ("Exposure2012", "Exposure"), ("Contrast2012", "Contrast"), ("Highlights2012", "Highlights"),
        ("Shadows2012", "Shadows"), ("Whites2012", "Whites"), ("Blacks2012", "Blacks"),
        ("Vibrance", "Vibrance"), ("Saturation", "Saturation"), ("Texture", "Texture"),
        ("Clarity2012", "Clarity"), ("Dehaze", "Dehaze"), ("Sharpness", "Sharpening"),
        ("LuminanceSmoothing", "NoiseReduction"), ("GrainAmount", "Grain")
    ];
    private static readonly string[] CurveProperties = ["ToneCurvePV2012", "ToneCurvePV2012Red", "ToneCurvePV2012Green", "ToneCurvePV2012Blue"];
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static XDocument Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        if (xml.Length > MaximumBytes || Encoding.UTF8.GetByteCount(xml) > MaximumBytes) throw new InvalidDataException("XMP exceeds the 16 MiB limit.");
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
            MaxCharactersInDocument = MaximumBytes, MaxCharactersFromEntities = 0, IgnoreComments = true
        };
        try
        {
            using (var input = new StringReader(xml)) using (var reader = XmlReader.Create(input, settings))
                while (reader.Read()) if (reader.Depth > 64) throw new InvalidDataException("XMP nesting exceeds 64 levels.");
            using var text = new StringReader(xml); using var parser = XmlReader.Create(text, settings);
            return XDocument.Load(parser, LoadOptions.None);
        }
        catch (XmlException error) { throw new InvalidDataException("Invalid or prohibited XMP XML: " + error.Message, error); }
    }
}
