using System.Text.Json;
using LightSpace.Core;
using LightSpace.Catalog;

var results = new List<object>(); var failed = 0; var passed = 0;
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine($"PASS {name}"); results.Add(new { name, passed = true, error = "" }); }
    catch (Exception error) { failed++; Console.WriteLine($"FAIL {name}: {error}"); results.Add(new { name, passed = false, error = error.Message }); }
}
CoreRenderingTests.Register(Test);
ParityTests.Register(Test);
AdvancedEditingTests.Register(Test);
XmpTests.Register(Test);
ColorRangeTests.Register(Test);
foreach (var legacy in new[] { 1, 2, 3 })
{
    Test($"Schema {legacy} migrates with neutral brush and channel settings", () =>
    {
        var source = new CatalogDocument();
        var photo = Fixtures.Tiny(); source.Photos.Add(photo); source.ActivePhoto = photo.Id;
        var json = CatalogSerializer.Serialize(source).Replace("\"SchemaVersion\":4", $"\"SchemaVersion\":{legacy}");
        var migrated = CatalogSerializer.Deserialize(json);
        Fixtures.Check(migrated.SchemaVersion == 4 && migrated.Photos[0].State.Develop.Channels.IsIdentity && migrated.Photos[0].State.Masks.Length == 0);
    });
}
foreach (var (name, action) in RecoveryTests.Cases.Concat(IncrementalRecoveryTests.Cases))
{
    try { await action(); passed++; Console.WriteLine($"PASS {name}"); results.Add(new { name, passed = true, error = "" }); }
    catch (Exception error) { failed++; Console.WriteLine($"FAIL {name}: {error}"); results.Add(new { name, passed = false, error = error.Message }); }
}
Test("Warm metadata edits avoid shader rebuilds and decodes", PerformanceChecks.Run);
Directory.CreateDirectory("artifacts/engine");
File.WriteAllText("artifacts/engine/results.json", JsonSerializer.Serialize(new { passed, failed, results }, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllBytes("artifacts/engine/import-fixture.png", Fixtures.Tiny().Original);
Console.WriteLine($"\n{passed} passed; {failed} failed.");
return failed == 0 ? 0 : 1;
