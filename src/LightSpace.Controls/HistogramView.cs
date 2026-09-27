using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

public sealed class HistogramView : SKCanvasElement
{
    private Histogram _histogram=Histogram.Empty;
    public Histogram Histogram { get=>_histogram;set{_histogram=value;Invalidate();} }
    public HistogramView(){Height=88;AutomationProperties.SetName(this,"RGB histogram");}
    protected override void RenderOverride(SKCanvas canvas,Size area)
    {
        var w=(float)area.Width;var h=(float)area.Height;using var p=new SKPaint{IsAntialias=true,Color=SKColor.Parse("#1d1d1d")};canvas.DrawRect(0,0,w,h,p);
        p.Color=SKColor.Parse("#343434");p.StrokeWidth=1;for(var i=1;i<4;i++)canvas.DrawLine(w*i/4,0,w*i/4,h,p);
        var max=Math.Max(1,Math.Max(_histogram.Red.Max(),Math.Max(_histogram.Green.Max(),_histogram.Blue.Max())));
        int[][] channels=[_histogram.Red,_histogram.Green,_histogram.Blue];SKColor[] colors=[new(221,96,98,135),new(128,192,136,135),new(105,144,221,135)];
        for(var channel=0;channel<3;channel++)
        {
            using var path=new SKPath();path.MoveTo(0,h);
            for(var i=0;i<256;i++){var value=channels[channel][i];var y=h-(float)(Math.Log(1+value)/Math.Log(1+max))*(h-5);path.LineTo(i*w/255,y);}path.LineTo(w,h);path.Close();p.Color=colors[channel];p.BlendMode=SKBlendMode.Screen;canvas.DrawPath(path,p);
        }
    }
}
