namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private int _mixerBand;
    private void ShowInspector(string mode){_inspectorMode=mode;BuildInspector();PublishDiagnostics();}
    private void BuildInspector()
    {
        _inspector.Children.Clear();_sliders.Clear();_curve=null;
        var heading=new Grid{Padding=new(18,16,12,12),ColumnDefinitions={new(){Width=new(1,GridUnitType.Star)},new(){Width=GridLength.Auto}}};
        heading.Children.Add(Theme.Text(_inspectorMode=="History"?"Versions":_inspectorMode,20));
        var action=Row();
        if(_inspectorMode=="Edit")
        {
            action.Children.Add(Button("Auto tone",Glyph.None,"Auto",()=>{if(Session.Active is{} p)Session.Edit("Auto tone",s=>s with{Develop=_renderer.Auto(p)});}));
            action.Children.Add(Button("Black and white",Glyph.None,"B&W",()=>Session.Edit("Black and white",s=>s with{Develop=s.Develop with{Monochrome=!s.Develop.Monochrome}})));
        }
        else action.Children.Add(Button("Back to editing",Glyph.Edit,null,()=>ChooseTool(PhotoTool.Edit)));
        Grid.SetColumn(action,1);heading.Children.Add(action);_inspector.Children.Add(heading);
        if(Session.Active is not{} photo){_inspector.Children.Add(Note("Import a photograph to start editing."));return;}
        switch(_inspectorMode)
        {
            case "Presets":BuildPresets();return;
            case "History":BuildHistory(photo);return;
            case "Info":BuildInfo(photo);return;
            case "Crop":BuildCrop(photo);return;
            case "Masks":BuildMasks(photo);return;
            case "Clone":BuildClone(photo);return;
        }
        _histogram.Margin=new(18,0,18,10);_inspector.Children.Add(_histogram);
        var profile=Row();profile.Margin=new(18,0,18,15);profile.Children.Add(Theme.Text("Profile",11,true));profile.Children.Add(Theme.Text(photo.State.Develop.Monochrome?"LightSpace Monochrome":"LightSpace Color",12));_inspector.Children.Add(profile);
        var light=new StackPanel();foreach(var name in new[]{"Exposure","Contrast","Highlights","Shadows","Whites","Blacks"})light.Children.Add(DevelopSlider(name));_inspector.Children.Add(new PanelSection("Light",light));
        var curve=new StackPanel();_curve=new ToneCurveView{Curve=photo.State.Develop.Curve};Register("tone-curve",_curve);_curve.CurveChanged+=value=>Session.Preview(s=>s with{Develop=s.Develop with{Curve=value}});_curve.Committed+=()=>Session.CommitGesture("Point curve");_curve.Canceled+=Session.CancelGesture;curve.Children.Add(_curve);curve.Children.Add(Note("Drag the five points. Double-click to reset."));_inspector.Children.Add(new PanelSection("Point curve",curve,false));
        var color=new StackPanel();var wb=Row();wb.Margin=new(0,0,0,8);wb.Children.Add(Theme.Text("White balance",11,true));wb.Children.Add(Button("Reset white balance",Glyph.None,"As imported",()=>Session.Edit("Reset white balance",s=>s with{Develop=s.Develop with{Temperature=0,Tint=0}})));color.Children.Add(wb);
        foreach(var name in new[]{"Temperature","Tint","Vibrance","Saturation"})color.Children.Add(DevelopSlider(name));_inspector.Children.Add(new PanelSection("Color",color));
        _inspector.Children.Add(new PanelSection("Color mixer",BuildMixer(photo),false));
        var effects=new StackPanel();foreach(var name in new[]{"Texture","Clarity","Dehaze","Vignette","Grain"})effects.Children.Add(DevelopSlider(name));_inspector.Children.Add(new PanelSection("Effects",effects,false));
        var detail=new StackPanel();detail.Children.Add(DevelopSlider("Sharpening"));detail.Children.Add(DevelopSlider("NoiseReduction"));detail.Children.Add(Note("Fast spatial smoothing, not AI denoising."));_inspector.Children.Add(new PanelSection("Detail",detail,false));
        var commands=new StackPanel{Margin=new(12,12,12,14),Spacing=5};var copy=Row();copy.Children.Add(Button("Copy edit settings",Glyph.None,"Copy",()=>{_clipboard=Session.Active?.State;SetStatus("Edit settings copied in this workspace.");}));copy.Children.Add(Button("Paste edit settings",Glyph.None,"Paste",PasteSettings));copy.Children.Add(Button("Sync selected photos",Glyph.Link,"Sync",Session.SyncSelected));commands.Children.Add(copy);
        commands.Children.Add(Button("Reset all edits",Glyph.Undo,"Reset edits",()=>Session.Edit("Reset edits",s=>s with{Develop=new(),Crop=new(),Masks=[],CloneSpots=[]})));_inspector.Children.Add(commands);
    }
    private AdjustmentSlider DevelopSlider(string name)
    {
        var nonnegative=name is "Grain" or "Sharpening" or "NoiseReduction";
        var label=name=="NoiseReduction"?"Noise reduction":name;
        var slider=new AdjustmentSlider(label,name=="Exposure"?-5:nonnegative?0:-100,name=="Exposure"?5:100,0,name=="Exposure"?.01:1){Value=Session.Active?.State.Develop.Get(name)??0};
        if(name=="Temperature"){slider.StartColor=SKColor.Parse("#397cbf");slider.EndColor=SKColor.Parse("#d4bc54");}
        if(name=="Tint"){slider.StartColor=SKColor.Parse("#47976d");slider.EndColor=SKColor.Parse("#ba6baa");}
        slider.ValueChanged+=value=>Session.Preview(s=>s with{Develop=s.Develop.Set(name,value)});slider.ValueCommitted+=()=>Session.CommitGesture(label);slider.GestureCanceled+=Session.CancelGesture;
        _sliders[name]=slider;Register("slider-"+name,slider.TrackElement);return slider;
    }
    private static TextBlock Note(string text){var note=Theme.Text(text,11,true);note.TextWrapping=TextWrapping.Wrap;note.TextTrimming=TextTrimming.None;note.Margin=new(0,8,0,10);return note;}
    private UIElement BuildMixer(PhotoDocument photo)
    {
        var root=new StackPanel();var bands=Row();bands.Spacing=2;var host=new StackPanel();
        string[] names=["Red","Orange","Yellow","Green","Aqua","Blue","Purple","Magenta"];
        string[] colors=["#bc6262","#d69b60","#c9b66b","#70a775","#6bafa9","#6b8cbd","#9277b6","#b374a7"];
        void ShowBand()
        {
            host.Children.Clear();var state=Session.Active?.State.Develop.Mixer[_mixerBand]??new ColorBand();host.Children.Add(Theme.Text(names[_mixerBand],11,true));
            foreach(var key in new[]{"Hue","Saturation","Luminance"})
            {
                var slider=new AdjustmentSlider(key){Value=key=="Hue"?state.Hue:key=="Saturation"?state.Saturation:state.Luminance};
                slider.ValueChanged+=value=>Session.Preview(s=>s with{Develop=s.Develop with{Mixer=s.Develop.Mixer.Select((b,i)=>i!=_mixerBand?b:key=="Hue"?b with{Hue=value}:key=="Saturation"?b with{Saturation=value}:b with{Luminance=value}).ToArray()}});
                slider.ValueCommitted+=()=>Session.CommitGesture(names[_mixerBand]+" "+key);slider.GestureCanceled+=Session.CancelGesture;host.Children.Add(slider);
            }
        }
        for(var i=0;i<8;i++){var index=i;var b=new LightButton(names[i]+" mixer",action:()=>{_mixerBand=index;ShowBand();}){Width=28,MinWidth=28,Padding=new(5),Content=new Border{Width=16,Height=16,CornerRadius=new(8),Background=Theme.Brush(colors[i])}};bands.Children.Add(b);}
        root.Children.Add(bands);root.Children.Add(host);ShowBand();return root;
    }
    private void BuildPresets()
    {
        _inspector.Children.Add(new Border{Margin=new(18,0,18,12),Child=Note("A considered starting point. Presets change development settings while retaining your crop and local masks.")});
        foreach(var group in BuiltInPresets.All.GroupBy(p=>p.Group))
        {
            var panel=new StackPanel{Spacing=4};foreach(var preset in group)
            {
                var button=Button("Preset "+preset.Name,Glyph.Presets,preset.Name,()=>{Session.Edit("Preset: "+preset.Name,s=>s with{Develop=preset.Settings});SetStatus("Applied "+preset.Name);});button.HorizontalAlignment=HorizontalAlignment.Stretch;button.HorizontalContentAlignment=HorizontalAlignment.Left;panel.Children.Add(button);
            }
            _inspector.Children.Add(new PanelSection(group.Key,panel));
        }
    }
    private void BuildHistory(PhotoDocument photo)
    {
        var versions=new StackPanel{Spacing=5};versions.Children.Add(Button("Save version",Glyph.Add,"Create named version",()=>Run(SaveVersionAsync)));
        foreach(var version in photo.Versions.AsEnumerable().Reverse())versions.Children.Add(Button("Restore "+version.Name,Glyph.History,version.Name,()=>Session.Edit("Restore version: "+version.Name,_=>version.State)));
        if(photo.Versions.Count==0)versions.Children.Add(Note("Preserve a look before exploring another direction."));_inspector.Children.Add(new PanelSection("Named versions",versions));
        var history=new StackPanel{Spacing=8};var buttons=Row();buttons.Children.Add(Button("History undo",Glyph.Undo,"Undo",Session.Undo));buttons.Children.Add(Button("History redo",Glyph.Redo,"Redo",Session.Redo));history.Children.Add(buttons);
        foreach(var item in Session.History.Take(30))history.Children.Add(Theme.Text(item,12,true));if(!Session.CanUndo)history.Children.Add(Note("Your original image is unchanged."));_inspector.Children.Add(new PanelSection("Edit history",history));
    }
    private void BuildInfo(PhotoDocument photo)
    {
        var info=new StackPanel{Spacing=12,Margin=new(18,0,18,18)};
        var filename=Theme.Text(photo.Name,14);filename.TextWrapping=TextWrapping.Wrap;info.Children.Add(filename);info.Children.Add(Theme.Text($"{photo.Width:N0} × {photo.Height:N0} pixels",12,true));info.Children.Add(Theme.Text($"{photo.Original.Length/1048576d:0.0} MiB · source retained",11,true));
        if(!string.IsNullOrEmpty(photo.Camera))info.Children.Add(Note(photo.Camera));if(!string.IsNullOrEmpty(photo.ExposureInfo))info.Children.Add(Note(photo.ExposureInfo));
        info.Children.Add(Theme.Text("Caption",11,true));var caption=Theme.Input("Add a caption","Photo caption",photo.State.Caption);caption.AcceptsReturn=true;caption.Height=84;caption.TextWrapping=TextWrapping.Wrap;info.Children.Add(caption);
        info.Children.Add(Theme.Text("Keywords",11,true));var keywords=Theme.Input("landscape, mountains, travel","Photo keywords",string.Join(", ",photo.State.Keywords));info.Children.Add(keywords);
        info.Children.Add(Button("Save photo information",Glyph.Check,"Save information",()=>Session.Edit("Photo information",s=>s with{Caption=caption.Text,Keywords=keywords.Text.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries)})));
        if(Session.Catalog.Albums.Count>0){info.Children.Add(Theme.Text("Add selection to album",11,true));foreach(var album in Session.Catalog.Albums)info.Children.Add(Button("Add to "+album.Name,Glyph.Folder,album.Name,()=>{Session.AddSelectionToAlbum(album.Id);SetStatus("Added selection to "+album.Name);}));}
        info.Children.Add(Note("Edits and catalog metadata are stored locally. Image export currently strips source EXIF/IPTC metadata; catalog backups retain the original bytes."));_inspector.Children.Add(info);
    }
    private void BuildCrop(PhotoDocument photo)
    {
        var panel=new StackPanel{Spacing=10,Margin=new(18,0,18,18)};panel.Children.Add(Note("Drag a new crop, resize its handles, or move an existing crop. Changes are non-destructive; Escape cancels the active gesture."));
        panel.Children.Add(Theme.Text("Aspect ratio",12,true));
        foreach(var (label,ratio) in new[]{("Original",0d),("1 × 1",1d),("4 × 3",4d/3),("3 × 2",1.5d),("16 × 9",16d/9)})
        {
            panel.Children.Add(Button("Crop "+label,Glyph.None,label,()=>
            {
                var w=1f;var h=1f;if(ratio>0){var original=(double)photo.Width/photo.Height;if(original>ratio)w=(float)(ratio/original);else h=(float)(original/ratio);}
                Session.Edit("Crop "+label,s=>s with{Crop=s.Crop with{Left=(1-w)/2,Right=(1+w)/2,Top=(1-h)/2,Bottom=(1+h)/2}});
            }));
        }
        var transform=Row();transform.Children.Add(Button("Rotate right",Glyph.Rotate,null,()=>Session.Edit("Rotate right",s=>s with{Crop=s.Crop with{QuarterTurns=s.Crop.QuarterTurns+1}})));transform.Children.Add(Button("Flip horizontal",Glyph.Flip,null,()=>Session.Edit("Flip horizontal",s=>s with{Crop=s.Crop with{FlipX=!s.Crop.FlipX}})));transform.Children.Add(Button("Flip vertical",Glyph.None,"Flip Y",()=>Session.Edit("Flip vertical",s=>s with{Crop=s.Crop with{FlipY=!s.Crop.FlipY}})));panel.Children.Add(transform);
        panel.Children.Add(Button("Reset crop",Glyph.Undo,"Reset crop",()=>Session.Edit("Reset crop",s=>s with{Crop=new()})));panel.Children.Add(Button("Apply crop",Glyph.Check,"Done",()=>ChooseTool(PhotoTool.Edit)));_inspector.Children.Add(panel);
    }
    private void BuildMasks(PhotoDocument photo)
    {
        var panel=new StackPanel{Spacing=8,Margin=new(18,0,18,18)};
        panel.Children.Add(Note("Drag across the photograph to create a mask. Up to eight radial or vertical linear gradients can be combined."));
        var tools=Row();tools.Children.Add(Button("Radial gradient",Glyph.Mask,"Radial",()=>{Viewport.SetTool(PhotoTool.RadialMask);BuildInspector();}));tools.Children.Add(Button("Linear gradient",Glyph.None,"Linear",()=>{Viewport.SetTool(PhotoTool.LinearMask);BuildInspector();}));panel.Children.Add(tools);
        panel.Children.Add(Button("Toggle mask overlay",Glyph.Mask,"Toggle outline",()=>{Viewport.MaskOverlay=!Viewport.MaskOverlay;Viewport.Invalidate();}));
        for(var i=0;i<photo.State.Masks.Length;i++){var index=i;panel.Children.Add(Button("Select mask "+i,Glyph.Mask,photo.State.Masks[i].Name,()=>{Viewport.SetActiveMask(index);BuildInspector();}));}
        if(photo.State.Masks.Length>0)
        {
            var index=Math.Clamp(Viewport.ActiveMask,0,photo.State.Masks.Length-1);var mask=photo.State.Masks[index];
            void Add(string label,float value,double min,double max,double step,Func<LocalMask,float,LocalMask> edit)
            {
                var slider=new AdjustmentSlider(label,min,max,0,step){Value=value};slider.ValueChanged+=v=>Session.Preview(s=>s with{Masks=s.Masks.Select((m,i)=>i==index?edit(m,v):m).ToArray()});slider.ValueCommitted+=()=>Session.CommitGesture("Mask "+label);slider.GestureCanceled+=Session.CancelGesture;Register("mask-"+label,slider.TrackElement);panel.Children.Add(slider);
            }
            Add("Exposure",mask.Exposure,-5,5,.01,(m,v)=>m with{Exposure=v});Add("Saturation",mask.Saturation,-100,100,1,(m,v)=>m with{Saturation=v});Add("Feather",mask.Feather*100,1,100,1,(m,v)=>m with{Feather=v/100});
            Add("Horizontal position",mask.X*100,0,100,1,(m,v)=>m with{X=v/100});Add("Vertical position",mask.Y*100,0,100,1,(m,v)=>m with{Y=v/100});
            panel.Children.Add(Button("Invert mask",Glyph.Compare,"Invert",()=>Session.Edit("Invert mask",s=>s with{Masks=s.Masks.Select((m,i)=>i==index?m with{Inverted=!m.Inverted}:m).ToArray()})));
            panel.Children.Add(Button("Delete mask",Glyph.Trash,"Delete mask",()=>Session.Edit("Delete mask",s=>s with{Masks=s.Masks.Where((_,i)=>i!=index).ToArray()})));
        }
        _inspector.Children.Add(panel);
    }
    private void BuildClone(PhotoDocument photo)
    {
        var panel=new StackPanel{Spacing=10,Margin=new(18,0,18,18)};panel.Children.Add(Note("Alt-click sets the source. Click a destination to clone from that source. Feathered circular stamps retain the original image. This is a clone tool, not generative removal."));
        var radius=new AdjustmentSlider("Size (%)",.2,25,4,.1){Value=Viewport.CloneRadius*100};radius.ValueChanged+=value=>Viewport.CloneRadius=value/100;panel.Children.Add(radius);
        panel.Children.Add(Theme.Text($"{photo.State.CloneSpots.Length} / 32 spots",11,true));panel.Children.Add(Button("Remove last clone spot",Glyph.Undo,"Undo last edit",Session.Undo));panel.Children.Add(Button("Clear clone spots",Glyph.Trash,"Clear spots",()=>Session.Edit("Clear clone spots",s=>s with{CloneSpots=[]})));_inspector.Children.Add(panel);
    }
    private void PasteSettings()
    {
        if(_clipboard is not{} source){SetStatus("Copy edit settings from a photograph first.");return;}
        Session.Edit("Paste edit settings",s=>s with{Develop=source.Develop,Crop=source.Crop,Masks=source.Masks,CloneSpots=source.CloneSpots},true);
    }
}
