using LightSpace.Core;
using LightSpace.Editing;
using static Fixtures;

internal static class VirtualCopySelectionTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Removing the active copy selects a survivor instead of an unselected master", () =>
        {
            var catalog = Catalog(2); var roots = catalog.Photos.ToArray();
            var session = new EditorSession(catalog);
            session.Select(roots[1].Id); session.Selection.Add(roots[0].Id);
            var copies = session.CreateVirtualCopies(roots.Select(p => p.Id));
            var removed = session.Active!; var survivor = copies.Single(p => p.Id != removed.Id);
            session.RemoveVirtualCopies([removed.Id]);
            Check(session.Active == survivor && session.Selection.SetEquals([survivor.Id]));
            session.Edit("Rate displayed survivor", state => state with { Rating = 4 }, selected: true);
            Check(survivor.State.Rating == 4 && roots.All(p => p.State.Rating == 0));
            Check(!catalog.Photos.Contains(removed) && roots.All(catalog.Photos.Contains));
        });
        test("Copy removal undo and redo restore active-selection handoffs exactly", () =>
        {
            var catalog = Catalog(3); var roots = catalog.Photos.ToArray();
            var session = new EditorSession(catalog);
            session.Select(roots[2].Id); session.Selection.UnionWith(roots.Select(p => p.Id));
            var copies = session.CreateVirtualCopies(roots.Select(p => p.Id));
            var removed = session.Active!; var before = session.Selection.ToArray();
            var survivors = copies.Where(p => p.Id != removed.Id).ToArray();
            var revision = session.Revision;
            session.RemoveVirtualCopies([removed.Id]);
            Check(session.Revision == revision + 1 && session.Active == survivors[0]);
            Check(session.Selection.SetEquals(survivors.Select(p => p.Id)));
            session.Undo(); Check(session.Active == removed && session.Selection.SetEquals(before));
            session.Redo(); Check(session.Active == survivors[0] && session.Selection.SetEquals(survivors.Select(p => p.Id)));
        });
        test("Removing an inactive copy does not move the retained active selection", () =>
        {
            var catalog = Catalog(2); var roots = catalog.Photos.ToArray();
            var session = new EditorSession(catalog); var copy = session.CreateVirtualCopies([roots[0].Id]).Single();
            session.Select(roots[1].Id); session.Selection.UnionWith([roots[0].Id, copy.Id]);
            session.RemoveVirtualCopies([copy.Id]);
            Check(session.Active == roots[1] && session.Selection.SetEquals(roots.Select(p => p.Id)));
            session.Undo(); Check(session.Active == roots[1] && session.Selection.Contains(copy.Id));
        });
    }
}
