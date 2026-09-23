using Avalonia.Controls;
using Avalonia.Controls.Templates;
using HTools.App.ViewModels;

namespace HTools.App;

/// <summary>Maps a view model instance to its view by naming convention:
/// HTools.App.ViewModels.FooViewModel -> HTools.App.Views.FooView.</summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "(null)" };
        }

        var name = data.GetType().FullName!
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        var type = Type.GetType(name);
        if (type is not null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is LocalizedViewModelBase;
}
