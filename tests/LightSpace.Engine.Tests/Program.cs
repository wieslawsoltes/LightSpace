using System.Text.Json;

var results = new List<object>(); var failed = 0; var passed = 0;
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine($"PASS {name}"); results.Add(new { name, passed = true, error = "" }); }
    catch (Exception error) { failed++; Console.WriteLine($"FAIL {name}: {error}"); results.Add(new { name, passed = false, error = error.Message }); }
}
CoreRenderingTests.Register(Test);
ParityTests.Register(Test);
AdvancedEditingTests.Register(Test);
foreach (var (name, action) in RecoveryTests.Cases)
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
