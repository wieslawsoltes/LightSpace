namespace LightSpace.Controls;

public sealed class PanelSection : StackPanel
{
    private readonly FrameworkElement _body;
    private readonly LightButton _header;
    private readonly string _title;
    public bool Expanded { get; private set; }
    public PanelSection(string title,FrameworkElement body,bool expanded=true)
    {
        _title=title;_body=body;Expanded=expanded;
        Children.Add(Theme.Divider());_header=new(title+" section",Glyph.None,title,Toggle){HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Left,Padding=new(18,12,18,12),CornerRadius=new(0),MinHeight=44};
        Children.Add(_header);_body.Margin=new(19,0,19,14);Children.Add(_body);Refresh();
    }
    public void Toggle(){Expanded=!Expanded;Refresh();}
    private void Refresh(){_body.Visibility=Expanded?Visibility.Visible:Visibility.Collapsed;_header.Text=(Expanded?"−  ":"+  ")+_title;}
}
