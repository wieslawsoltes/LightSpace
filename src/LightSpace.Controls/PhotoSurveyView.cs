using LightSpace.Core;
using LightSpace.Rendering.Skia;
namespace LightSpace.Controls;

/// <summary>
/// Up to twelve photographs drawn directly through one Uno/Skia surface.
/// A separate bounded renderer prevents survey visits evicting detail previews.
/// Owner-thread preparation is amortized to one photo per timer tick.
/// </summary>
public sealed class PhotoSurveyView : UserControl, IDisposable
{
    private sealed class Surface(PhotoSurveyView owner) : SKCanvasElement
    {
        protected override void RenderOverride(SKCanvas canvas, Size area) => owner.Paint(canvas);
    }
    private sealed class Entry(PhotoDocument photo, SurveyCard card)
    {
        public PhotoDocument Photo = photo;
        public PhotoState State = photo.State;
        public byte[] Original = photo.Original;
        public int Width = photo.Width, Height = photo.Height;
        public SurveyCard Card = card;
        public bool Ready, Failed;
    }
    public const int PreviewDimension = 1024;
    public const long DecodedBudget = (long)SurveyLayout.MaximumVisiblePhotos * PreviewDimension * PreviewDimension * 4;
    private PhotoRenderer? _renderer;
    private readonly Surface _surface;
    private readonly Canvas _chrome = new();
    private readonly DispatcherTimer _prepare = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
    private SurveyTile[] _layout = [];
    private bool _active, _disposed, _before;
    private double _layoutWidth = -1, _layoutHeight = -1;
    private int _prepared, _cardBuilds, _layoutBuilds;
    public IReadOnlyDictionary<string, FrameworkElement> Widgets => _widgets;
    public IReadOnlyList<SurveyTile> Tiles => _layout;
    public RendererStatistics Statistics => _renderer?.Statistics ?? new(0, 0, 0, 0, 0, 0);
    public int ReadyCount => _entries.Count(e => e.Ready);
    public int FailedCount => _entries.Count(e => e.Failed);
    public int PreparationSteps => _prepared;
    public int CardBuilds => _cardBuilds;
    public int LayoutBuilds => _layoutBuilds;
    public event Action<Guid>? Activated;
    public event Action<Guid>? OpenRequested;
    public event Action<Guid>? RatingRequested;
    public event Action<Guid, PhotoFlag>? FlagRequested;
    public event Action<Guid>? ExcludeRequested;
    public event Action<string>? Status;
    public bool Before { get => _before; set { if (_before == value) return; _before = value; _surface.Invalidate(); } }

    public PhotoSurveyView()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        _surface = new(this); var root = new Grid(); root.Children.Add(_surface); root.Children.Add(_chrome); Content = root;
        AutomationProperties.SetName(this, "Survey photographs");
        SizeChanged += (_, _) => ArrangePhotos();
        Loaded += (_, _) => Schedule(); Unloaded += (_, _) => _prepare.Stop();
        _prepare.Tick += PrepareNext;
    }
    public void SetActive(bool active)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _active = active;
        if (active) Schedule();
        else
        {
            _prepare.Stop(); _renderer?.Clear();
            foreach (var e in _entries) { e.Ready = e.Failed = false; e.Card.SetReady(false); }
        }
    }
    public void SetPhotos(IReadOnlyList<PhotoDocument> photos, Guid activeId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ArgumentNullException.ThrowIfNull(photos);
        if (photos.Count > SurveyLayout.MaximumVisiblePhotos || photos.Any(p => p is null || p.Id == Guid.Empty || p.Width <= 0 || p.Height <= 0)
            || photos.Select(p => p.Id).Distinct().Count() != photos.Count)
            throw new ArgumentException("A survey page requires distinct photos within its capacity.", nameof(photos));
        var same = photos.Count == _entries.Count && photos.Select(p => p.Id).SequenceEqual(_entries.Select(e => e.Photo.Id));
        var layoutDirty = !same; var pixelsDirty = !same;
        if (!same)
        {
            var retained = photos.Select(p => p.Id).ToHashSet();
            var previous = _entries.ToDictionary(e => e.Photo.Id);
            foreach (var e in _entries.Where(e => !retained.Contains(e.Photo.Id))) _renderer?.ReleasePhoto(e.Photo.Id);
            _entries.Clear(); _chrome.Children.Clear(); _widgets.Clear();
            foreach (var photo in photos)
            {
                if (!previous.TryGetValue(photo.Id, out var e))
                {
                    var card = new SurveyCard(photo); e = new(photo, card); _cardBuilds++;
                    card.Activated += id => Activated?.Invoke(id); card.OpenRequested += id => OpenRequested?.Invoke(id);
                    card.RatingRequested += id => RatingRequested?.Invoke(id); card.FlagRequested += (id, flag) => FlagRequested?.Invoke(id, flag);
                    card.ExcludeRequested += id => ExcludeRequested?.Invoke(id);
                }
                _entries.Add(e); _chrome.Children.Add(e.Card);
                var index = _entries.Count - 1;
                _widgets["survey-photo-" + index] = e.Card.HitTarget;
                var names = new[] { "rating", "pick", "reject", "exclude" };
                for (var action = 0; action < names.Length; action++) _widgets[$"survey-{names[action]}-{index}"] = e.Card.Actions[action];
            }
        }
        for (var i = 0; i < photos.Count; i++)
        {
            var e = _entries[i]; var photo = photos[i];
            if (!ReferenceEquals(e.Original, photo.Original) || !PhotoStateEquality.Pixels(e.State, photo.State))
            { e.Ready = e.Failed = false; pixelsDirty = true; }
            if (e.State.Crop != photo.State.Crop || e.Width != photo.Width || e.Height != photo.Height) layoutDirty = true;
            e.Photo = photo; e.State = photo.State; e.Original = photo.Original; e.Width = photo.Width; e.Height = photo.Height;
            e.Card.Update(photo, photo.Id == activeId); e.Card.SetReady(e.Ready, e.Failed);
        }
        if (layoutDirty) { _layoutWidth = -1; ArrangePhotos(); }
        else if (pixelsDirty) _surface.Invalidate();
        Schedule();
    }
    public void RetryFailed()
    {
        foreach (var e in _entries.Where(e => e.Failed)) { e.Failed = false; e.Card.SetReady(false); }
        Schedule();
    }
    private void Schedule()
    {
        if (!_disposed && _active && IsLoaded && !_prepare.IsEnabled && _entries.Any(e => !e.Ready && !e.Failed)) _prepare.Start();
    }
    private void PrepareNext(object? sender, object e)
    {
        if (!_active || _disposed) { _prepare.Stop(); return; }
        var next = _entries.FirstOrDefault(entry => !entry.Ready && !entry.Failed);
        if (next is null) { _prepare.Stop(); return; }
        try
        {
            _renderer ??= new PhotoRenderer(PreviewDimension, SurveyLayout.MaximumVisiblePhotos, DecodedBudget);
            _renderer.Prepare(next.Photo); next.Ready = true; _prepared++;
        }
        catch (Exception error) { next.Failed = true; Status?.Invoke($"{next.Photo.Name}: {error.Message}"); }
        next.Card.SetReady(next.Ready, next.Failed); _surface.Invalidate();
        if (_entries.All(entry => entry.Ready || entry.Failed)) _prepare.Stop();
    }
    private void ArrangePhotos()
    {
        if (_disposed || ActualWidth < 1 || ActualHeight < 1) return;
        var width = Math.Max(0, ActualWidth - 24); var height = Math.Max(0, ActualHeight - 24);
        if (width == _layoutWidth && height == _layoutHeight) return;
        var input = _entries.Select(e =>
        {
            var size = e.Photo.State.Crop.OutputSize(e.Photo.Width, e.Photo.Height);
            return new SurveyImage(e.Photo.Id, (double)size.Width / size.Height);
        }).ToArray();
        _layout = SurveyLayout.Arrange(input, width, height); _layoutWidth = width; _layoutHeight = height; _layoutBuilds++;
        for (var i = 0; i < _layout.Length; i++)
        {
            var tile = _layout[i]; var card = _entries[i].Card;
            Canvas.SetLeft(card, tile.Bounds.X + 12); Canvas.SetTop(card, tile.Bounds.Y + 12);
            card.Width = tile.Bounds.Width; card.Height = tile.Bounds.Height;
            card.SetFooter(tile.Bounds.Height - tile.ImageBounds.Height);
        }
        _surface.Invalidate();
    }
    private void Paint(SKCanvas canvas)
    {
        canvas.Clear(SKColor.Parse("#171717"));
        if (!_active || _renderer is null) return;
        for (var i = 0; i < Math.Min(_layout.Length, _entries.Count); i++)
        {
            var entry = _entries[i]; if (!entry.Ready) continue;
            var b = _layout[i].ImageBounds;
            if (b.Width <= 1 || b.Height <= 1) continue;
            var available = SKRect.Create((float)b.X + 14, (float)b.Y + 14, Math.Max(1, (float)b.Width - 4), Math.Max(1, (float)b.Height - 4));
            var target = PhotoTransform.Fit(entry.Photo.State.Crop, entry.Photo.Width, entry.Photo.Height, available);
            try { _renderer.Draw(canvas, entry.Photo, target, original: Before); }
            catch (Exception error) { entry.Ready = false; entry.Failed = true; Status?.Invoke($"{entry.Photo.Name}: {error.Message}"); }
        }
    }
    public new void Dispose()
    {
        if (_disposed) return; _disposed = true; _prepare.Stop(); _renderer?.Dispose(); _entries.Clear(); _chrome.Children.Clear(); _widgets.Clear();
    }
}
