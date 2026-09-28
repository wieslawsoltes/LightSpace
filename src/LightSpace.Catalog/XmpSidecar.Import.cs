using System.Globalization;
using System.Xml.Linq;
using LightSpace.Core;
namespace LightSpace.Catalog;

public static partial class XmpSidecar
{
    public static XmpImportResult Import(string xml, PhotoState? baseline = null, XmpImportOptions? options = null)
    {
        options ??= new(); var document = Parse(xml); var warnings = new List<string>(); var applied = new List<string>();
        var descriptions = document.Descendants(Rdf + "Description").ToArray();
        if (descriptions.Length == 0 || descriptions.Length > 128) throw new InvalidDataException("Expected an XMP RDF description.");
        var subjects = descriptions.Select(d => (string?)d.Attribute(Rdf + "about") ?? "").Distinct().ToArray();
        if (subjects.Length > 1) throw new InvalidDataException("A sidecar must describe one resource, not multiple subjects.");
        string? Read(XName name)
        {
            var values = descriptions.SelectMany(d => d.Attributes(name).Select(a => a.Value).Concat(d.Elements(name).Where(e => !e.HasElements).Select(e => e.Value))).Distinct().ToArray();
            if (values.Length > 1) throw new InvalidDataException($"Conflicting XMP values for {name.LocalName}.");
            return values.FirstOrDefault();
        }
        var state = (baseline ?? new()).Normalize(); var native = false;
        var nativeJson = Read(Native + "Settings");
        if (!options.MetadataOnly && options.PreferLightSpaceSettings && nativeJson is not null)
        {
            if (Read(Native + "SchemaVersion") != NativeSchemaVersion.ToString(Invariant)) throw new InvalidDataException("Unsupported LightSpace sidecar settings version.");
            state = CatalogSerializer.DeserializeSettings(nativeJson); native = true; applied.Add("LightSpace complete settings");
            warnings.Add("The LightSpace extension takes precedence over Camera Raw development values. Standard metadata is still read from the packet.");
        }
        if (Read(Xmp + "Rating") is { } rating)
        {
            if (float.TryParse(rating, NumberStyles.Float, Invariant, out var value) && float.IsFinite(value) && value is >= -1 and <= 5)
            {
                if (value != MathF.Round(value)) warnings.Add("Fractional XMP rating was rounded to whole stars.");
                state = value == -1 ? state with { Flag = PhotoFlag.Reject } : state with { Rating = (int)MathF.Round(value), Flag = state.Flag == PhotoFlag.Reject ? PhotoFlag.None : state.Flag };
                applied.Add("Rating");
            }
            else warnings.Add("Invalid XMP rating was ignored.");
        }
        if (Read(Xmp + "Label") is { } label) { state = state with { Label = label }; applied.Add("Label"); }
        var captionElement = descriptions.SelectMany(d => d.Elements(Dc + "description")).FirstOrDefault();
        if (captionElement is not null)
        {
            var alternatives = captionElement.Element(Rdf + "Alt")?.Elements(Rdf + "li").ToArray() ?? [];
            var caption = alternatives.FirstOrDefault(e => (string?)e.Attribute(XNamespace.Xml + "lang") == "x-default")?.Value ?? alternatives.FirstOrDefault()?.Value ?? captionElement.Value;
            state = state with { Caption = caption }; applied.Add("Caption");
        }
        var keywordElements = descriptions.SelectMany(d => d.Elements(Dc + "subject")).ToArray();
        if (keywordElements.Length > 0)
        {
            var keywords = keywordElements.SelectMany(e => e.Elements().Where(e => e.Name == Rdf + "Bag" || e.Name == Rdf + "Seq").Elements(Rdf + "li")).Select(e => e.Value).ToArray();
            state = state with { Keywords = keywords }; applied.Add("Keywords");
        }
        var crsNames = descriptions.SelectMany(d => d.Attributes().Where(a => a.Name.Namespace == Crs).Select(a => a.Name.LocalName)
            .Concat(d.Elements().Where(e => e.Name.Namespace == Crs).Select(e => e.Name.LocalName))).Distinct().ToArray();
        if (!options.MetadataOnly && !native)
        {
            foreach (var (property, setting) in NumericProperties)
            {
                if (Read(Crs + property) is not { } text) continue;
                if (!float.TryParse(text, NumberStyles.Float, Invariant, out var value) || !float.IsFinite(value)) { warnings.Add($"Invalid {property} was ignored."); continue; }
                var develop = state.Develop.Set(setting, value);
                if (develop.Get(setting) != value) warnings.Add($"{property} was clamped to LightSpace's supported range.");
                state = state with { Develop = develop }; applied.Add(property);
            }
            if (Read(Crs + "ConvertToGrayscale") is { } monochrome)
            {
                if (bool.TryParse(monochrome, out var value)) { state = state with { Develop = state.Develop with { Monochrome = value } }; applied.Add("ConvertToGrayscale"); }
                else warnings.Add("Invalid ConvertToGrayscale was ignored.");
            }
            var channels = state.Develop.Channels; var importedCurve = false;
            for (var channel = 0; channel < CurveProperties.Length; channel++)
            {
                var name = CurveProperties[channel]; var element = descriptions.SelectMany(d => d.Elements(Crs + name)).FirstOrDefault();
                if (element is null) continue;
                var entries = element.Element(Rdf + "Seq")?.Elements(Rdf + "li").ToArray() ?? [];
                if (entries.Length is < 2 or > PointCurve.MaximumPoints) { warnings.Add($"{name} requires 2–32 points; it was ignored."); continue; }
                var points = new List<CurvePoint>();
                foreach (var entry in entries)
                {
                    var pair = entry.Value.Split(',');
                    if (pair.Length != 2 || !float.TryParse(pair[0], NumberStyles.Float, Invariant, out var x) || !float.TryParse(pair[1], NumberStyles.Float, Invariant, out var y)
                        || !float.IsFinite(x) || !float.IsFinite(y) || x is < 0 or > 255 || y is < 0 or > 255) { points.Clear(); break; }
                    points.Add(new(x / 255, y / 255));
                }
                if (points.Count < 2) { warnings.Add($"Malformed {name} was ignored."); continue; }
                var source = new PointCurve { Points = points.ToArray() }; var normalized = source.Normalize();
                if (!source.Points.AsSpan().SequenceEqual(normalized.Points)) warnings.Add($"{name} point ordering or endpoints were normalized.");
                channels = channels.Set((CurveChannel)channel, normalized); importedCurve = true; applied.Add(name);
            }
            if (importedCurve) state = state with { Develop = state.Develop with { Channels = channels, Curve = new() } };
            if (crsNames.Length > 0) warnings.Add("Camera Raw values are mapped to LightSpace's original processing model; identical Adobe-rendered pixels are not guaranteed.");
        }
        var supported = NumericProperties.Select(p => p.Property).Concat(CurveProperties).Concat(new[] { "ConvertToGrayscale", "Version", "ProcessVersion", "HasSettings", "ToneCurveName2012" }).ToHashSet(StringComparer.Ordinal);
        if (!native && !options.MetadataOnly)
            foreach (var name in crsNames.Where(n => !supported.Contains(n)).Order()) warnings.Add("Unsupported Camera Raw property: " + name);
        if (options.MetadataOnly && (crsNames.Length > 0 || nativeJson is not null)) warnings.Add("Development settings were excluded by metadata-only import.");
        return new(state.Normalize(), applied.Distinct().ToArray(), warnings.Distinct().ToArray(), native);
    }
}
