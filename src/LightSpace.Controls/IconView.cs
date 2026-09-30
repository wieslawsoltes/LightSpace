namespace LightSpace.Controls;

public enum Glyph { None, Menu, Grid, Photo, Edit, Crop, Mask, Clone, Presets, Info, Folder, Add, Import, Export, Undo, Redo, Search, Star, Flag, Reject, Check, Chevron, Close, Compare, Rotate, Flip, Help, Cloud, History, Zoom, Trash, Link, RejectFlag }
public sealed class IconView : SKCanvasElement
{
    private SKColor _color = SKColor.Parse("#c4c4c4");
    public Glyph Glyph { get; set; }
    public SKColor Color { get => _color; set { _color = value; Invalidate(); } }
    public IconView(Glyph glyph) { Glyph = glyph; Width = 20; Height = 20; IsHitTestVisible = false; }
    protected override void RenderOverride(SKCanvas canvas, Size area)
    {
        canvas.Save(); canvas.Scale((float)area.Width / 24, (float)area.Height / 24);
        using var p = new SKPaint { Color = Color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
        void Line(float x1, float y1, float x2, float y2) => canvas.DrawLine(x1,y1,x2,y2,p);
        void Path(params float[] points) { using var path = new SKPath(); path.MoveTo(points[0],points[1]); for (var i=2;i<points.Length;i+=2) path.LineTo(points[i],points[i+1]); canvas.DrawPath(path,p); }
        switch(Glyph)
        {
            case Glyph.Menu: Line(4,6,20,6);Line(4,12,20,12);Line(4,18,20,18);break;
            case Glyph.Grid: foreach(var x in new[]{4,14}) foreach(var y in new[]{4,14}) canvas.DrawRect(x,y,6,6,p);break;
            case Glyph.Photo: canvas.DrawRoundRect(new(3,4,21,20),1,1,p);canvas.DrawCircle(8,9,1.5f,p);Path(4,18,10,12,14,16,17,12,21,17);break;
            case Glyph.Edit: Line(4,6,20,6);Line(4,12,20,12);Line(4,18,20,18);canvas.DrawCircle(9,6,2,p);canvas.DrawCircle(16,12,2,p);canvas.DrawCircle(7,18,2,p);break;
            case Glyph.Crop: Path(7,3,7,17,21,17);Path(3,7,17,7,17,21);Line(10,14,21,3);break;
            case Glyph.Mask: using(var dash=SKPathEffect.CreateDash([2f,2f],0)){p.PathEffect=dash;canvas.DrawCircle(12,12,8,p);}break;
            case Glyph.Clone: Path(7,15,9,11,9,5,15,5,15,11,17,15,7,15);Path(5,19,5,16,19,16,19,19);break;
            case Glyph.Presets: canvas.DrawCircle(9,12,7,p);canvas.DrawCircle(15,12,7,p);break;
            case Glyph.Info: case Glyph.Help: canvas.DrawCircle(12,12,9,p);Line(12,11,12,17);canvas.DrawCircle(12,7,0.6f,p);break;
            case Glyph.Folder: Path(3,7,3,20,21,20,21,8,12,8,10,5,3,5,3,7);break;
            case Glyph.Add: Line(12,4,12,20);Line(4,12,20,12);break;
            case Glyph.Import: Path(5,15,5,20,19,20,19,15);Line(12,3,12,15);Path(8,11,12,15,16,11);break;
            case Glyph.Export: Path(5,15,5,20,19,20,19,15);Line(12,3,12,15);Path(8,7,12,3,16,7);break;
            case Glyph.Undo: Path(8,4,3,9,8,14);Path(3,9,15,9,19,12,19,17);break;
            case Glyph.Redo: Path(16,4,21,9,16,14);Path(21,9,9,9,5,12,5,17);break;
            case Glyph.Search: case Glyph.Zoom: canvas.DrawCircle(10,10,6,p);Line(15,15,21,21);break;
            case Glyph.Star: using(var star=new SKPath()){for(var i=0;i<10;i++){var r=i%2==0?9:4;var a=-Math.PI/2+i*Math.PI/5;var x=12+(float)Math.Cos(a)*r;var y=12+(float)Math.Sin(a)*r;if(i==0)star.MoveTo(x,y);else star.LineTo(x,y);}star.Close();canvas.DrawPath(star,p);}break;
            case Glyph.Flag: Path(6,21,6,3,18,3,15,8,18,12,6,12);break;
            case Glyph.RejectFlag: Path(6,21,6,3,18,3,15,8,18,12,6,12);Line(3,2,21,17);break;
            case Glyph.Reject: case Glyph.Close: Line(6,6,18,18);Line(18,6,6,18);break;
            case Glyph.Check: Path(4,12,9,17,20,6);break;
            case Glyph.Chevron: Path(8,5,15,12,8,19);break;
            case Glyph.Compare: canvas.DrawRect(3,5,18,14,p);Line(12,2,12,22);break;
            case Glyph.Rotate: canvas.DrawArc(new(5,5,20,20),-80,275,false,p);Path(6,2,5,7,10,6);break;
            case Glyph.Flip: Line(12,2,12,22);Path(9,6,3,18,9,18,9,6);Path(15,6,21,18,15,18,15,6);break;
            case Glyph.Cloud: Path(6,18,3,15,4,11,8,10,9,6,14,5,18,9,21,12,20,17,6,18);break;
            case Glyph.History: canvas.DrawCircle(12,12,8,p);Path(12,7,12,12,16,14);break;
            case Glyph.Trash: Path(7,7,8,21,16,21,17,7);Line(4,5,20,5);Line(10,2,14,2);break;
            case Glyph.Link: canvas.DrawOval(new(3,8,14,16),p);canvas.DrawOval(new(10,8,21,16),p);break;
        }
        canvas.Restore();
    }
}
