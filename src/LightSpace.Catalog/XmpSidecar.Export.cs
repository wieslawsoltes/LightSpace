using System.Text;
using System.Xml.Linq;
using LightSpace.Core;
namespace LightSpace.Catalog;

public static partial class XmpSidecar
{
    public static XmpExportResult Export(PhotoState input, bool includeDevelopment = true, bool includeNativeSettings = true)
    {
        var state = input.Normalize(); var warnings = new List<string>();
        var description = new XElement(Rdf + "Description", new XAttribute(Rdf + "about", ""),
            new XAttribute(XNamespace.Xmlns + "xmp", Xmp.NamespaceName), new XAttribute(XNamespace.Xmlns + "dc", Dc.NamespaceName),
            new XAttribute(Xmp + "CreatorTool", "LightSpace"), new XAttribute(Xmp + "Rating", state.Flag == PhotoFlag.Reject ? -1 : state.Rating), new XAttribute(Xmp + "Label", state.Label),
            new XElement(Dc + "description", new XElement(Rdf + "Alt", new XElement(Rdf + "li", new XAttribute(XNamespace.Xml + "lang", "x-default"), state.Caption))),
            new XElement(Dc + "subject", new XElement(Rdf + "Bag", state.Keywords.Select(k => new XElement(Rdf + "li", k)))));
        if (includeDevelopment)
        {
            description.Add(new XAttribute(XNamespace.Xmlns + "crs", Crs.NamespaceName), new XAttribute(Crs + "HasSettings", "True"));
            foreach (var (property, setting) in NumericProperties) description.Add(new XAttribute(Crs + property, state.Develop.Get(setting).ToString("R", Invariant)));
            description.Add(new XAttribute(Crs + "ConvertToGrayscale", state.Develop.Monochrome ? "True" : "False"), new XAttribute(Crs + "ToneCurveName2012", "Custom"));
            for (var i = 0; i < 4; i++)
            {
                var curve = state.Develop.Channels.Get((CurveChannel)i); var points = curve.Points;
                if (i == 0 && !state.Develop.Curve.IsIdentity)
                {
                    var master = curve.Compile(); points = Enumerable.Range(0, 32).Select(x => new CurvePoint(x / 31f, master.Evaluate(state.Develop.Curve.Evaluate(x / 31f)))).ToArray();
                    warnings.Add("The combined legacy/master curve was sampled to 32 points for the Camera Raw subset.");
                }
                var quantized = points.Select(p => (X: (int)MathF.Round(p.X * 255), Y: (int)MathF.Round(p.Y * 255))).GroupBy(p => p.X).Select(g => g.Last());
                description.Add(new XElement(Crs + CurveProperties[i], new XElement(Rdf + "Seq", quantized.Select(p => new XElement(Rdf + "li", $"{p.X}, {p.Y}")))));
            }
            warnings.Add("Camera Raw export is a parameter subset, not Adobe processing parity. Curve coordinates are quantized to 0–255.");
            if (state.Masks.Length > 0 || state.CloneSpots.Length > 0 || state.Crop != new CropSettings() || !state.Develop.Grading.IsNeutral
                || state.Develop.Temperature != 0 || state.Develop.Tint != 0 || state.Develop.Vignette != 0 || !state.Geometry.IsIdentity || !state.Optics.IsNeutral || state.Geometry.ConstrainCrop)
                warnings.Add("Masks, cloning, crop, geometry, optics, grading, relative white balance and vignette require the native extension; they are not represented by the standard Camera Raw subset.");
            if (includeNativeSettings)
            {
                description.Add(new XAttribute(XNamespace.Xmlns + "ls", Native.NamespaceName), new XAttribute(Native + "SchemaVersion", NativeSchemaVersion),
                    new XElement(Native + "Settings", CatalogSerializer.SerializeSettings(state)));
            }
        }
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(XName.Get("xmpmeta", "adobe:ns:meta/"), new XAttribute(XNamespace.Xmlns + "x", "adobe:ns:meta/"),
                new XElement(Rdf + "RDF", new XAttribute(XNamespace.Xmlns + "rdf", Rdf.NamespaceName), description)));
        var xml = document.ToString(SaveOptions.DisableFormatting);
        if (Encoding.UTF8.GetByteCount(xml) > MaximumBytes) throw new InvalidDataException("This sidecar exceeds the 16 MiB limit; export a catalog backup instead.");
        return new(xml, warnings.ToArray());
    }
}
