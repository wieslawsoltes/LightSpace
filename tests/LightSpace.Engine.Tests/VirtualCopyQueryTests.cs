using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Editing;
using static Fixtures;

internal static class VirtualCopyQueryTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Original and virtual-copy filters combine with album, rating and text", () =>
        {
            var s = new EditorSession(Catalog(2)); var root = s.Active!;
            var album = s.CreateAlbum("Looks");
            var copy = s.CreateVirtualCopies([root.Id]).Single(); s.RenameVirtualCopy(copy.Id, "Evening");
            s.Edit("Metadata", state => state with { Rating = 4, Flag = PhotoFlag.Pick });
            Check(new PhotoQuery { Kind = PhotoKindFilter.Originals }.Execute(s.Catalog).Count == 2);
            Check(new PhotoQuery("even", 4, PhotoFlag.Pick, album.Id) { Kind = PhotoKindFilter.VirtualCopies }.Execute(s.Catalog).Single() == copy);
            Check(new PhotoQuery("even") { Kind = PhotoKindFilter.Originals }.Execute(s.Catalog).Count == 0);
            Check(new PhotoQuery { Kind = PhotoKindFilter.VirtualCopies }.Execute(s.Catalog).Count == 1);
        });
        test("Virtual copies keep stable adjacency in import order", () =>
        {
            var s = new EditorSession(Catalog(2)); var roots = s.Catalog.Photos.ToArray();
            var copies = s.CreateVirtualCopies(roots.Select(p => p.Id));
            var ordered = new PhotoQuery().Execute(s.Catalog);
            Check(ordered.Select(p => p.Id).SequenceEqual(new[] { roots[0].Id, copies[0].Id, roots[1].Id, copies[1].Id }));
        });
        test("Legacy original null copy name normalizes before query execution", () =>
        {
            var catalog = Catalog(1); catalog.Photos[0].CopyName = null!;
            var normalized = CatalogSerializer.Deserialize(CatalogSerializer.Serialize(catalog));
            Check(normalized.Photos[0].CopyName == "" && new PhotoQuery("Photo").Execute(normalized).Count == 1);
        });
        test("XMP settings remain processing version five and do not encode copy identity", () =>
        {
            var s = new EditorSession(Catalog(1)); var copy = s.CreateVirtualCopies([s.Active!.Id]).Single();
            s.Edit("Alternative", state => state with { Develop = state.Develop with { Exposure = 1.2f } });
            var xml = XmpSidecar.Export(copy.State).Xml;
            Check(XmpSidecar.NativeSchemaVersion == 5 && !xml.Contains("MasterPhotoId") && !xml.Contains("CopyName"));
            Check(PhotoStateEquality.All(XmpSidecar.Import(xml).State, copy.State));
        });
    }
}
