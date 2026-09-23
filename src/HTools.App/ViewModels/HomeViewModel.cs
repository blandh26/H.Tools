using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class HomeViewModel : LocalizedViewModelBase
{
    // Tools whose card shows an inline on/off toggle wired straight to their live page view model.
    private static readonly HashSet<string> QuickToggleToolIds = ["mouse-effect"];

    private readonly Action<ToolDescriptor> _openTool;
    private readonly Func<ToolDescriptor, object> _pageFactory;

    [ObservableProperty]
    private string _groupKey = NavGroupKeys.Home;

    public HomeViewModel(ILocalizationService loc, Action<ToolDescriptor> openTool, Func<ToolDescriptor, object> pageFactory)
        : base(loc)
    {
        _openTool = openTool;
        _pageFactory = pageFactory;
        Cards = [];
    }

    public ObservableCollection<ToolCardViewModel> Cards { get; }

    /// <summary>The Home nav group has no tools of its own assigned to it (it exists purely as a
    /// landing page), so unlike every other group — which shows only its own tools — Home shows every
    /// available tool across all groups as a simple overview/dashboard.</summary>
    public void ShowGroup(string groupKey)
    {
        GroupKey = groupKey;
        Cards.Clear();
        var descriptors = groupKey == NavGroupKeys.Home
            ? ToolCatalog.All.Where(d => d.IsAvailable)
            : ToolCatalog.ForGroup(groupKey);
        foreach (var descriptor in descriptors)
        {
            Cards.Add(new ToolCardViewModel(descriptor, Loc, QuickToggleTargetFor(descriptor)));
        }
    }

    private object? QuickToggleTargetFor(ToolDescriptor descriptor) =>
        descriptor.IsAvailable && QuickToggleToolIds.Contains(descriptor.Id) ? _pageFactory(descriptor) : null;

    [RelayCommand]
    private void OpenTool(ToolCardViewModel card) => _openTool(card.Descriptor);
}
