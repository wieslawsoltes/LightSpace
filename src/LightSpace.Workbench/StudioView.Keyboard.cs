namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private void Keyboard(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || (XamlRoot is { } root && FocusManager.GetFocusedElement(root) is TextBox)) return;
        if (_dialogOverlay is not null) { if (e.Key == VirtualKey.Escape) { CloseDialog(); e.Handled = true; } return; }
        bool Down(VirtualKey key) => Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var control = Down(VirtualKey.Control) || Down(VirtualKey.LeftWindows) || Down(VirtualKey.RightWindows);
        var shift = Down(VirtualKey.Shift); var handled = true;
        if (control)
        {
            switch (e.Key)
            {
                case VirtualKey.Z: if (shift) Session.Redo(); else Session.Undo(); break;
                case VirtualKey.Y: Session.Redo(); break;
                case VirtualKey.I: Run(ImportAsync); break;
                case VirtualKey.S: Run(SaveCatalogAsync); break;
                case VirtualKey.E: Run(ShowExportAsync); break;
                case VirtualKey.A: Session.Selection.UnionWith(_visible.Select(p => p.Id)); RefreshCatalog(); break;
                case VirtualKey.C: _clipboard = Session.Active?.State; SetStatus("Edit settings copied."); break;
                case VirtualKey.V: PasteSettings(); break;
                default: handled = false; break;
            }
        }
        else if (e.Key >= VirtualKey.Number0 && e.Key <= VirtualKey.Number5)
        { var rating = (int)e.Key - (int)VirtualKey.Number0; Session.Edit("Rating", s => s with { Rating = rating }, true); }
        else switch (e.Key)
        {
            case VirtualKey.G: SetGrid(true); break;
            case VirtualKey.E: case VirtualKey.D: ChooseTool(PhotoTool.Edit); break;
            case VirtualKey.R: ChooseTool(PhotoTool.Crop); break;
            case VirtualKey.M: ChooseTool(PhotoTool.RadialMask); break;
            case VirtualKey.W: StartWhiteBalance(); break;
            case VirtualKey.J: SetClipping(Viewport.Clipping == ClippingIndicators.None ? ClippingIndicators.Both : ClippingIndicators.None); break;
            case VirtualKey.F6: ToggleFocusMode(); break;
            case VirtualKey.F7: ToggleFilmstrip(); break;
            case VirtualKey.Z: Viewport.ToggleZoom(); break;
            case VirtualKey.Y: Viewport.Compare = !Viewport.Compare; Viewport.Invalidate(); break;
            case VirtualKey.P: Session.Edit("Pick", s => s with { Flag = PhotoFlag.Pick }, true); break;
            case VirtualKey.X: Session.Edit("Reject", s => s with { Flag = PhotoFlag.Reject }, true); break;
            case VirtualKey.U: Session.Edit("Clear flag", s => s with { Flag = PhotoFlag.None }, true); break;
            case VirtualKey.Left: Navigate(-1); break;
            case VirtualKey.Right: Navigate(1); break;
            case VirtualKey.Escape: ChooseTool(PhotoTool.Edit); break;
            default: if ((int)e.Key == 220) { Viewport.Before = !Viewport.Before; Viewport.Invalidate(); } else handled = false; break;
        }
        e.Handled = handled;
    }
}
