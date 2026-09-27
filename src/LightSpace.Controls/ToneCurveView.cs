using LightSpace.Core;
namespace LightSpace.Controls;

public sealed class ToneCurveView : SKCanvasElement
{
    private ToneCurve _curve=new();private bool _dragging;private int _index;
    public ToneCurve Curve{get=>_curve;set{_curve=value;Invalidate();}}
    public event Action<ToneCurve>? CurveChanged;public event Action? Committed;public event Action? Canceled;
    public ToneCurveView()
    {
        Height=200;AutomationProperties.SetName(this,"Point curve");
        PointerPressed+=(_,e)=>{_dragging=true;var p=e.GetCurrentPoint(this).Position;_index=Math.Clamp((int)Math.Round((p.X-10)/Math.Max(1,ActualWidth-20)*4),0,4);CapturePointer(e.Pointer);Move(e);e.Handled=true;};
        PointerMoved+=(_,e)=>{if(_dragging)Move(e);};PointerReleased+=(_,e)=>{if(!_dragging)return;Move(e);_dragging=false;ReleasePointerCapture(e.Pointer);Committed?.Invoke();};
        PointerCanceled+=(_,_)=>Cancel();PointerCaptureLost+=(_,_)=>Cancel();
        DoubleTapped+=(_,_)=>{Curve=new();CurveChanged?.Invoke(Curve);Committed?.Invoke();};
    }
    private void Cancel(){if(!_dragging)return;_dragging=false;Canceled?.Invoke();}
    private void Move(PointerRoutedEventArgs e)
    {
        var y=Numeric.Unit((float)(1-(e.GetCurrentPoint(this).Position.Y-10)/Math.Max(1,ActualHeight-20)));
        Curve=_index switch{0=>Curve with{Black=y},1=>Curve with{Shadow=y},2=>Curve with{Mid=y},3=>Curve with{Light=y},_=>Curve with{White=y}};CurveChanged?.Invoke(Curve);
    }
    protected override void RenderOverride(SKCanvas canvas,Size area)
    {
        var w=(float)area.Width-20;var h=(float)area.Height-20;canvas.Save();canvas.Translate(10,10);using var p=new SKPaint{Color=SKColor.Parse("#191919"),IsAntialias=true};canvas.DrawRect(0,0,w,h,p);
        p.Style=SKPaintStyle.Stroke;p.Color=SKColor.Parse("#3c3c3c");p.StrokeWidth=1;for(var i=0;i<=4;i++){canvas.DrawLine(w*i/4,0,w*i/4,h,p);canvas.DrawLine(0,h*i/4,w,h*i/4,p);}canvas.DrawLine(0,h,w,0,p);
        using var path=new SKPath();path.MoveTo(0,h*(1-Curve.Black));path.LineTo(w*.25f,h*(1-Curve.Shadow));path.LineTo(w*.5f,h*(1-Curve.Mid));path.LineTo(w*.75f,h*(1-Curve.Light));path.LineTo(w,h*(1-Curve.White));p.Color=SKColor.Parse("#d2d2d2");p.StrokeWidth=1.5f;canvas.DrawPath(path,p);
        p.Style=SKPaintStyle.Fill;float[] values=[Curve.Black,Curve.Shadow,Curve.Mid,Curve.Light,Curve.White];for(var i=0;i<5;i++)canvas.DrawCircle(w*i/4,h*(1-values[i]),3,p);canvas.Restore();
    }
}
