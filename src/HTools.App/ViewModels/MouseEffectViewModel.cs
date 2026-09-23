using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Windows.Effects;
using HTools.Windows.Services;

namespace HTools.App.ViewModels;

public sealed partial class MouseEffectViewModel : LocalizedViewModelBase
{
    private readonly MouseEffectService _service;
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public MouseEffectViewModel(ILocalizationService loc, MouseEffectService service, AppSettingsContext settings)
        : base(loc)
    {
        _service = service;
        _settings = settings;

        Colors = new ObservableCollection<ColorOption>(BuildColorOptions());
        Shapes = new ObservableCollection<ShapeOption>(BuildShapeOptions());

        var current = settings.Current.MouseEffect;

        _suppressPersist = true;
        IsEnabled = current.Enabled;
        GhostCount = current.GhostCount;
        DurationMs = current.EffectDurationMs;
        SelectedColor = Colors.FirstOrDefault(c => c.Key == current.ColorName) ?? Colors[0];
        SelectedShape = Shapes.FirstOrDefault(s => s.Key == current.Shape) ?? Shapes[0];
        _suppressPersist = false;

        ApplyAllToService();
    }

    public ObservableCollection<ColorOption> Colors { get; }

    public ObservableCollection<ShapeOption> Shapes { get; }

    public string Title => Loc.Translate("MouseEffect.Title");

    public string EnableLabel => Loc.Translate("MouseEffect.Enable");

    public string GhostCountLabel => Loc.Translate("MouseEffect.GhostCount");

    public string DurationLabel => Loc.Translate("MouseEffect.Duration");

    public string ColorLabel => Loc.Translate("MouseEffect.Color");

    public string ShapeLabel => Loc.Translate("MouseEffect.Shape");

    public string TestPulseLabel => Loc.Translate("MouseEffect.TestPulse");

    public string Hint => Loc.Translate("MouseEffect.Hint");

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private int _ghostCount;

    [ObservableProperty]
    private int _durationMs;

    [ObservableProperty]
    private ColorOption? _selectedColor;

    [ObservableProperty]
    private ShapeOption? _selectedShape;

    [RelayCommand]
    private void TestPulse() => _service.TestPulse();

    partial void OnIsEnabledChanged(bool value)
    {
        _service.SetEnabled(value);
        Persist(s => s.Enabled = value);
    }

    partial void OnGhostCountChanged(int value)
    {
        _service.SetGhostCount(value);
        Persist(s => s.GhostCount = value);
    }

    partial void OnDurationMsChanged(int value)
    {
        _service.SetEffectDuration(value);
        Persist(s => s.EffectDurationMs = value);
    }

    partial void OnSelectedColorChanged(ColorOption? value)
    {
        if (value is null)
        {
            return;
        }

        var match = EffectColor.All.First(c => c.Name == value.Key);
        _service.SetColor(match.Color);
        Persist(s => s.ColorName = value.Key);
    }

    partial void OnSelectedShapeChanged(ShapeOption? value)
    {
        if (value is null)
        {
            return;
        }

        _service.SetShape(Enum.Parse<MouseGhostShape>(value.Key));
        Persist(s => s.Shape = value.Key);
    }

    private void ApplyAllToService()
    {
        _service.SetGhostCount(GhostCount);
        _service.SetEffectDuration(DurationMs);
        if (SelectedColor is not null)
        {
            _service.SetColor(EffectColor.All.First(c => c.Name == SelectedColor.Key).Color);
        }

        if (SelectedShape is not null)
        {
            _service.SetShape(Enum.Parse<MouseGhostShape>(SelectedShape.Key));
        }

        _service.SetEnabled(IsEnabled);
    }

    private ShapeOption[] BuildShapeOptions() =>
    [
        new ShapeOption(nameof(MouseGhostShape.PlumBlossom), Loc.Translate("MouseEffect.Shape.PlumBlossom")),
        new ShapeOption(nameof(MouseGhostShape.Arrow), Loc.Translate("MouseEffect.Shape.Arrow")),
    ];

    private ColorOption[] BuildColorOptions() => EffectColor.All
        .Select(c => ColorOption.Create(
            c.Name,
            Loc.Translate($"MouseEffect.Color.{c.Name}"),
            Avalonia.Media.Color.FromArgb(c.Color.A, c.Color.R, c.Color.G, c.Color.B)))
        .ToArray();

    private void Persist(Action<Core.Models.MouseEffectSettings> apply)
    {
        if (_suppressPersist)
        {
            return;
        }

        apply(_settings.Current.MouseEffect);
        _settings.Save();
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(EnableLabel));
        OnPropertyChanged(nameof(GhostCountLabel));
        OnPropertyChanged(nameof(DurationLabel));
        OnPropertyChanged(nameof(ColorLabel));
        OnPropertyChanged(nameof(ShapeLabel));
        OnPropertyChanged(nameof(TestPulseLabel));
        OnPropertyChanged(nameof(Hint));

        var selectedShapeKey = SelectedShape?.Key;
        Shapes.Clear();
        foreach (var option in BuildShapeOptions())
        {
            Shapes.Add(option);
        }

        var selectedColorKey = SelectedColor?.Key;
        Colors.Clear();
        foreach (var option in BuildColorOptions())
        {
            Colors.Add(option);
        }

        _suppressPersist = true;
        SelectedShape = Shapes.FirstOrDefault(s => s.Key == selectedShapeKey) ?? Shapes[0];
        SelectedColor = Colors.FirstOrDefault(c => c.Key == selectedColorKey) ?? Colors[0];
        _suppressPersist = false;
    }
}
