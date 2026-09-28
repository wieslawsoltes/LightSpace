using System.Diagnostics;
using System.Text.Json;
using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Rendering.Skia;
using SkiaSharp;
using static Fixtures;

internal static class AdvancedEditingTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Point curves normalize endpoints, ordering and duplicate positions", () =>
        {
            var c = new PointCurve { Points = [new(.6f,.3f),new(.2f,2),new(.2f,.4f),new(float.NaN,1)] }.Normalize();
            Check(c.Points[0].X == 0 && c.Points[^1].X == 1 && c.Points.Length == 4);
            Check(c.Points.Zip(c.Points.Skip(1)).All(p => p.Second.X > p.First.X));
        });
        test("Compiled identity curve is identity", () =>
        {
            var c = new PointCurve().Compile(); for (var i = 0; i <= 1000; i++) Check(Math.Abs(c.Evaluate(i / 1000f) - i / 1000f) < .000001);
        });
        test("Smooth curve passes through every control point", () =>
        {
            var source = Curve(); var compiled = source.Compile();
            foreach (var point in source.Points) Check(Math.Abs(compiled.Evaluate(point.X) - point.Y) < .00001);
        });
        test("Shape-preserving interpolation does not overshoot non-monotone segments", () =>
        {
            var source = new PointCurve { Points = [new(0,.1f),new(.2f,.9f),new(.5f,.2f),new(.9f,.8f),new(1,.4f)] };
            var c = source.Compile();
            for (var j = 0; j < source.Points.Length - 1; j++) for (var i = 0; i <= 100; i++)
            {
                var a = source.Points[j]; var b = source.Points[j + 1]; var v = c.Evaluate(a.X + (b.X-a.X)*i/100);
                Check(v >= Math.Min(a.Y,b.Y)-.00001 && v <= Math.Max(a.Y,b.Y)+.00001);
            }
        });
        test("Linear curve interpolation matches segment arithmetic", () =>
        {
            var c = new PointCurve { Interpolation = CurveInterpolation.Linear, Points = [new(0,0),new(.5f,.8f),new(1,1)] }.Compile(); Check(Math.Abs(c.Evaluate(.25f)-.4f)<.00001);
        });
        test("Curve compile copies mutable caller points", () =>
        {
            var source = Curve(); var compiled = source.Compile(); var before = compiled.Evaluate(.5f);
            source.Points[1] = new(.3f,.99f); Check(compiled.Evaluate(.5f)==before);
        });
        test("Independent channel curve affects only its channel", () =>
        {
            var p = Solid(new(128,128,128)); p.State = new() { Develop = new() { Channels = new() { Red = Curve() } } };
            using var r = new PhotoRenderer(); using var bitmap = SKBitmap.Decode(r.Export(p,SKEncodedImageFormat.Png)); var c = bitmap.GetPixel(32,32);
            Check(Math.Abs(c.Red-255*Curve().Compile().Evaluate(128/255f))<3 && Math.Abs(c.Green-128)<2 && Math.Abs(c.Blue-128)<2);
        });
        test("Master and channel curves compose in documented order", () =>
        {
            var p = Solid(new(128,128,128)); var channels = new ChannelCurves { Master = Curve(), Blue = new() { Points=[new(0,0),new(.5f,.8f),new(1,1)] } };
            p.State = new() { Develop = new() { Channels = channels } }; using var r = new PhotoRenderer(); using var bitmap = SKBitmap.Decode(r.Export(p,SKEncodedImageFormat.Png));
            var expected = channels.Blue.Compile().Evaluate(channels.Master.Compile().Evaluate(128/255f)); Check(Math.Abs(bitmap.GetPixel(32,32).Blue-expected*255)<3);
        });
        test("Curve table is not rebuilt for exposure changes", () =>
        {
            using var r = new PhotoRenderer(); var p=Tiny(); p.State=new(){Develop=new(){Channels=new(){Master=Curve()}}};
            using var s=SKSurface.Create(new SKImageInfo(96,64)); r.Draw(s.Canvas,p,SKRect.Create(96,64)); var builds=r.CurveLookupBuilds;
            p.State=p.State with{Develop=p.State.Develop with{Exposure=1}}; r.Draw(s.Canvas,p,SKRect.Create(96,64)); Check(r.CurveLookupBuilds==builds);
        });
        test("Curve table retains floating-point precision", () =>
        {
            using var cache=new ToneLookupCache(); using var pixels=cache.Get(new(){Master=Curve()}).PeekPixels(); Check(pixels.Info.ColorType==SKColorType.RgbaF32);
        });
        test("Brush spatial coverage uses image aspect ratio", () =>
        {
            var m=Mask(); Check(m.Weight(.5f,.5f,.5f,2)>.99); Check(m.Weight(.6f,.5f,.5f,2)<.01); Check(m.Weight(.5f,.6f,.5f,2)>.9);
        });
        test("Brush flow and density cap accumulated coverage", () =>
        {
            var stroke=new BrushStroke{Flow=.5f,Density=.6f,Dabs=Enumerable.Repeat(new BrushDab(.5f,.5f),20).ToArray()};
            var value=BrushStroke.Apply(0,[stroke],.5f,.5f,1); Check(value<=.6f && value>.599f);
        });
        test("Erase subtracts without altering original stroke", () =>
        {
            var first=Mask().Strokes[0]; var erase=first with{Id=Guid.NewGuid(),Erase=true,Density=.5f};
            Check(Math.Abs(BrushStroke.Apply(0,[first,erase],.5f,.5f,1)-.5f)<.00001); Check(!first.Erase);
        });
        test("Brushes can add and subtract from analytic gradients", () =>
        {
            var stroke=Mask().Strokes[0]; var gradient=new LocalMask{Kind=MaskKind.Linear,Strokes=[stroke with{Erase=true}]}; Check(gradient.Weight(.5f,.5f)==0);
            gradient=gradient with{Strokes=[stroke]}; Check(gradient.Weight(.5f,.5f)>.99);
        });
        test("Brush range intersection and inversion follow spatial painting", () =>
        {
            var m=Mask() with{RangeEnabled=true,RangeMin=.3f,RangeMax=.7f}; Check(m.Weight(.5f,.5f,.1f)==0); Check((m with{Inverted=true}).Weight(.5f,.5f,.1f)==1);
        });
        test("Brush builder is invariant to collinear event subdivision", () =>
        {
            var a=new BrushStrokeBuilder(new(),1.5f);a.Add(.1f,.5f);a.Add(.9f,.5f);
            var b=new BrushStrokeBuilder(new(),1.5f);for(var i=0;i<=80;i++)b.Add(.1f+i*.01f,.5f);
            var x=a.Snapshot().Dabs;var y=b.Snapshot().Dabs; Check(x.Length==y.Length,$"{x.Length} vs {y.Length}");
            for(var i=0;i<x.Length;i++)Check(Math.Abs(x[i].X-y[i].X)<.00001);
        });
        test("Brush builder snapshots are immutable", () =>
        {
            var b=new BrushStrokeBuilder(new(),1);b.Add(.1f,.5f);var first=b.Snapshot();b.Add(.9f,.5f);Check(first.Dabs.Length==1 && b.Count>1);
        });
        test("Brush normalization rejects oversized inputs", () => Throws(()=>new BrushStroke{Dabs=new BrushDab[4097]}.Normalize()));
        test("Brush incremental cache equals a complete replay", () =>
        {
            var first=Mask();using var cache=new BrushCoverageCache();cache.Get(first,120,80);
            var grown=first with{Strokes=[first.Strokes[0] with{Dabs=[..first.Strokes[0].Dabs,new(.55f,.5f)]}]};
            using var incremental=SKBitmap.FromImage(cache.Get(grown,120,80));using var reference=new BrushCoverageCache();using var full=SKBitmap.FromImage(reference.Get(grown,120,80));Check(incremental.Pixels.SequenceEqual(full.Pixels));Check(cache.Statistics.DabsRasterized==2);
        });
        test("Brush cache affine texture matches scalar reference", () =>
        {
            var first=Mask();var m=first with{Strokes=[first.Strokes[0] with{Flow=.3f,Feather=.8f},first.Strokes[0] with{Erase=true,Flow=.4f,Dabs=[new(.54f,.5f)]}]};
            using var cache=new BrushCoverageCache();using var b=SKBitmap.FromImage(cache.Get(m,120,80));
            for(var y=0;y<80;y+=7)for(var x=0;x<120;x+=7){var p=b.GetPixel(x,y);var expected=BrushStroke.Apply(.4f,m.Strokes,(x+.5f)/120,(y+.5f)/80,1.5f);Check(Math.Abs((.4*p.Red+p.Green)/255-expected)<.006);}
        });
        test("Brush undo invalidates coverage rather than retaining later dabs", () =>
        {
            var m=Mask();using var c=new BrushCoverageCache();var original=c.Get(m,120,80);using var before=SKBitmap.FromImage(original);
            var changed=m with{Strokes=[..m.Strokes,m.Strokes[0] with{Erase=true}]};c.Get(changed,120,80);using var restored=SKBitmap.FromImage(c.Get(m,120,80));Check(restored.Pixels.SequenceEqual(before.Pixels));Check(c.Statistics.Replays==1);
        });
        test("Brush shader changes only painted pixels", () =>
        {
            var p=Solid(new(102,102,102),120,80);p.State=new(){Masks=[Mask()]};using var r=new PhotoRenderer();using var b=SKBitmap.Decode(r.Export(p,SKEncodedImageFormat.Png));Check(b.GetPixel(60,40).Red>130 && Math.Abs(b.GetPixel(1,1).Red-102)<2);
        });
        test("Brush parameters reuse coverage across local exposure changes", () =>
        {
            var p=Tiny();p.State=new(){Masks=[Mask()]};using var r=new PhotoRenderer();using var s=SKSurface.Create(new SKImageInfo(96,64));r.Draw(s.Canvas,p,SKRect.Create(96,64));var before=r.BrushStatistics;
            p.State=p.State with{Masks=[p.State.Masks[0] with{Exposure=2}]};r.Draw(s.Canvas,p,SKRect.Create(96,64));Check(r.BrushStatistics==before);
        });
        test("Brush and RGB curve settings roundtrip losslessly", () =>
        {
            var state=new PhotoState{Develop=new(){Channels=new(){Master=Curve()}},Masks=[Mask()]};var copy=CatalogSerializer.DeserializeSettings(CatalogSerializer.SerializeSettings(state));Check(PhotoStateEquality.All(state,copy));
        });
        test("Advanced editing work-avoidance counters", Performance);
    }
    private static PointCurve Curve()=>new(){Points=[new(0,0),new(.3f,.15f),new(.6f,.45f),new(1,1)]};
    private static LocalMask Mask()=>new(){Kind=MaskKind.Brush,Exposure=1,Strokes=[new(){Radius=.15f,Feather=0,Flow=1,Dabs=[new(.5f,.5f)]}]};
    private static void Performance()
    {
        var p=Solid(new(102,102,102),384,256);p.State=new(){Develop=new(){Channels=new(){Master=Curve()}},Masks=[Mask()]};
        using var r=new PhotoRenderer();using var s=SKSurface.Create(new SKImageInfo(96,64));r.Draw(s.Canvas,p,SKRect.Create(96,64));
        var before=r.BrushStatistics;var lookup=r.CurveLookupBuilds;var timer=Stopwatch.StartNew();
        for(var i=0;i<100;i++){p.State=p.State with{Develop=p.State.Develop with{Exposure=i/100f}};r.Draw(s.Canvas,p,SKRect.Create(96,64));}
        timer.Stop();Check(r.BrushStatistics==before && lookup==r.CurveLookupBuilds);
        Directory.CreateDirectory("artifacts/engine");File.WriteAllText("artifacts/engine/advanced-performance.json",JsonSerializer.Serialize(new{scope="100 warm exposure updates with brush and RGB curve; raster canvas, not GPU timing",elapsedMs=timer.Elapsed.TotalMilliseconds,additionalBrushDabs=r.BrushStatistics.DabsRasterized-before.DabsRasterized,additionalBrushTextures=r.BrushStatistics.TextureBuilds-before.TextureBuilds,additionalCurveTables=r.CurveLookupBuilds-lookup},new JsonSerializerOptions{WriteIndented=true}));
    }
}
