using System.Globalization;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
namespace LightSpace.Controls;

public sealed class AdjustmentSlider : UserControl
{
    private sealed class Track(AdjustmentSlider owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area)
        {
            var width = (float)area.Width; var y = (float)area.Height / 2; var left = 5f; var right = Math.Max(6,width-5);
            using var p = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = 2, Color = SKColor.Parse("#555555") };
            if(owner.StartColor is SKColor a && owner.EndColor is SKColor b)
            { using var shader=SKShader.CreateLinearGradient(new SKPoint(left,y),new SKPoint(right,y),new SKColor[]{a,b},null,SKShaderTileMode.Clamp);p.Shader=shader;canvas.DrawLine(left,y,right,y,p);p.Shader=null; }
            else canvas.DrawLine(left,y,right,y,p);
            var fraction=(float)((owner.Value-owner.Minimum)/(owner.Maximum-owner.Minimum));var x=left+fraction*(right-left);
            p.Color=SKColor.Parse("#909090");p.StrokeWidth=1;var zero=left+(float)((owner.DefaultValue-owner.Minimum)/(owner.Maximum-owner.Minimum))*(right-left);canvas.DrawLine(zero,y-3,zero,y+3,p);
            p.Color=SKColor.Parse("#dedede");p.Style=SKPaintStyle.Fill;canvas.DrawCircle(x,y,4,p);
            p.Style=SKPaintStyle.Stroke;p.Color=SKColor.Parse("#1d1d1d");p.StrokeWidth=1;canvas.DrawCircle(x,y,4,p);
        }
    }
    private readonly Track _track;
    private readonly TextBox _number;
    private double _value;
    private bool _dragging;
    private bool _typing;
    public double Minimum { get; }
    public double Maximum { get; }
    public double DefaultValue { get; }
    public double Step { get; }
    public SKColor? StartColor { get; set; }
    public SKColor? EndColor { get; set; }
    public FrameworkElement TrackElement => _track;
    public double Value { get => _value; set => Set(value, false); }
    public event Action<float>? ValueChanged;
    public event Action? ValueCommitted;
    public event Action? GestureCanceled;
    public AdjustmentSlider(string name,double min=-100,double max=100,double value=0,double step=1)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || max <= min) throw new ArgumentOutOfRangeException(nameof(max));
        if (!double.IsFinite(step) || step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        Minimum=min;Maximum=max;DefaultValue=Math.Clamp(value,min,max);Step=step;_value=DefaultValue;IsTabStop=true;
        AutomationProperties.SetName(this,name);AutomationProperties.SetAutomationId(this,name);
        var root=new Grid { RowDefinitions={new(){Height=GridLength.Auto},new(){Height=new(24)}} };
        var row=new Grid { ColumnDefinitions={new(){Width=new(1,GridUnitType.Star)},new(){Width=new(60)}} };
        var label=Theme.Text(name);label.Margin=new(0,0,0,0);row.Children.Add(label);
        _number=Theme.Input("",name+" value");_number.TextAlignment=TextAlignment.Right;_number.FontSize=11;_number.MinHeight=20;_number.Height=23;_number.Padding=new(3,0,3,0);_number.BorderThickness=new(0);_number.Background=Theme.Brush("#00000000");Grid.SetColumn(_number,1);row.Children.Add(_number);root.Children.Add(row);
        _track=new(this){Height=24};Grid.SetRow(_track,1);root.Children.Add(_track);Content=root;Margin=new(0,0,0,3);
        _track.PointerPressed+=(_,e)=>{_dragging=true;Focus(FocusState.Pointer);_track.CapturePointer(e.Pointer);FromPointer(e);e.Handled=true;};
        _track.PointerMoved+=(_,e)=>{if(_dragging){FromPointer(e);e.Handled=true;}};
        _track.PointerReleased+=(_,e)=>{if(!_dragging)return;FromPointer(e);_dragging=false;_track.ReleasePointerCapture(e.Pointer);ValueCommitted?.Invoke();e.Handled=true;};
        _track.PointerCanceled+=(_,_)=>Cancel();_track.PointerCaptureLost+=(_,_)=>Cancel();
        _track.DoubleTapped+=(_,e)=>{Set(DefaultValue,true);ValueCommitted?.Invoke();e.Handled=true;};
        _number.GotFocus+=(_,_)=>_typing=true;_number.LostFocus+=(_,_)=>CommitNumber();
        _number.KeyDown+=(_,e)=>{if(e.Key==VirtualKey.Enter){CommitNumber();Focus(FocusState.Programmatic);e.Handled=true;}else if(e.Key==VirtualKey.Escape){_typing=false;Refresh();Focus(FocusState.Programmatic);e.Handled=true;}};
        KeyDown+=(_,e)=>{if(e.Key==VirtualKey.Escape&&_dragging){Cancel();e.Handled=true;return;}if(_typing)return;var next=e.Key switch{VirtualKey.Left or VirtualKey.Down=>Value-Step,VirtualKey.Right or VirtualKey.Up=>Value+Step,VirtualKey.Home=>Minimum,VirtualKey.End=>Maximum,_=>double.NaN};if(double.IsFinite(next)){Set(next,true);ValueCommitted?.Invoke();e.Handled=true;}};
        Refresh();
    }
    private void FromPointer(PointerRoutedEventArgs e){var x=e.GetCurrentPoint(_track).Position.X;Set(Minimum+(x-5)/Math.Max(1,_track.ActualWidth-10)*(Maximum-Minimum),true);}
    private void Cancel(){if(!_dragging)return;_dragging=false;_track.ReleasePointerCaptures();GestureCanceled?.Invoke();}
    private void CommitNumber(){if(!_typing)return;_typing=false;if(double.TryParse(_number.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var number)&&double.IsFinite(number)){Set(number,true);ValueCommitted?.Invoke();}Refresh();}
    internal void Set(double value,bool notify)
    {
        if (!double.IsFinite(value)) return;
        value = Math.Clamp(Math.Round(value / Step) * Step, Minimum, Maximum);
        // A preview refreshes its sibling controls too. Do not rewrite native
        // input values or invalidate a track whose quantized value is unchanged.
        if (Math.Abs(_value - value) <= .000001) return;
        _value = value; Refresh(); if (notify) ValueChanged?.Invoke((float)value);
    }
    internal void SetFromAutomation(double value){Set(value,true);ValueCommitted?.Invoke();}
    private void Refresh()
    {
        if (!_typing)
        {
            var text = _value.ToString(Step < 1 ? "0.00" : "0", CultureInfo.InvariantCulture);
            if (_number.Text != text) _number.Text = text;
        }
        _track.Invalidate();
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new SliderPeer(this);
    private sealed class SliderPeer(AdjustmentSlider owner):FrameworkElementAutomationPeer(owner),IRangeValueProvider
    {
        protected override string GetClassNameCore()=>nameof(AdjustmentSlider);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Slider;
        protected override object? GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.RangeValue?this:base.GetPatternCore(pattern);
        public bool IsReadOnly=>!owner.IsEnabled;public double LargeChange=>(owner.Maximum-owner.Minimum)/10;public double Maximum=>owner.Maximum;public double Minimum=>owner.Minimum;public double SmallChange=>owner.Step;public double Value=>owner.Value;
        public void SetValue(double value){if(!owner.IsEnabled)throw new InvalidOperationException("Slider is disabled.");owner.SetFromAutomation(value);}
    }
}
