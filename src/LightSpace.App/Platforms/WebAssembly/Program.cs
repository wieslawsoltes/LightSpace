using LightSpace.Core;
using Uno.UI.Hosting;
namespace LightSpace.App;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // Diagnostic branch only: these comparisons execute before constructing
        // the application or invoking any Skia rendering API.
        Console.WriteLine("LIGHTSPACE_PROBE_START record-span");
        var a = Enumerable.Range(0, 8).Select(_ => new ColorBand()).ToArray();
        var b = Enumerable.Range(0, 8).Select(_ => new ColorBand()).ToArray();
        for (var i = 0; i < 10000; i++)
            if (!a.AsSpan().SequenceEqual(b)) throw new InvalidOperationException("Equal color bands differ.");
        Console.WriteLine("LIGHTSPACE_PROBE_PASS record-span");
        Console.WriteLine("LIGHTSPACE_PROBE_START curve-span");
        var firstCurve = new PointCurve(); var secondCurve = new PointCurve();
        for (var i = 0; i < 10000; i++)
            if (!firstCurve.ValueEquals(secondCurve)) throw new InvalidOperationException("Equal curves differ.");
        Console.WriteLine("LIGHTSPACE_PROBE_PASS curve-span");
        Console.WriteLine("LIGHTSPACE_PROBE_START develop-equality");
        var first = new DevelopSettings(); var second = new DevelopSettings();
        for (var i = 0; i < 10000; i++)
            if (!PhotoStateEquality.Develop(first, second)) throw new InvalidOperationException("Equal development differs.");
        Console.WriteLine("LIGHTSPACE_PROBE_PASS develop-equality");
        await UnoPlatformHostBuilder.Create().App(() => new App()).UseWebAssembly().Build().RunAsync();
    }
}
