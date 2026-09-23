using CommunityToolkit.Mvvm.ComponentModel;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

/// <summary>Base for view models with text pulled from <see cref="ILocalizationService"/> that must
/// refresh their bindings when the user switches language at runtime.</summary>
public abstract class LocalizedViewModelBase : ObservableObject
{
    protected LocalizedViewModelBase(ILocalizationService loc)
    {
        Loc = loc;
        Loc.LanguageChanged += (_, _) => OnLanguageChanged();
    }

    protected ILocalizationService Loc { get; }

    /// <summary>Override to raise PropertyChanged for every locale-dependent property.</summary>
    protected virtual void OnLanguageChanged()
    {
    }
}
