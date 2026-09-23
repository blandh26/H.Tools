using Avalonia.Controls;
using Avalonia.Input;
using HTools.App.ViewModels;

namespace HTools.App.Views;

public partial class ClipboardView : UserControl
{
    public ClipboardView()
    {
        InitializeComponent();
    }

    private void OnSlotDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (SlotsList.SelectedItem is ClipboardSlotViewModel slot
            && DataContext is ClipboardViewModel viewModel)
        {
            viewModel.EditSlotCommand.Execute(slot);
        }
    }
}
