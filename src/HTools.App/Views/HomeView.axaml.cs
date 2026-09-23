using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Templates;
using HTools.App.ViewModels;
using HTools.Core.Models;

namespace HTools.App.Views;

public partial class HomeView : UserControl
{
    private static readonly DataFormat<string> ToolCardIdFormat = DataFormat.CreateInProcessFormat<string>("HTools.ToolCardId");

    public HomeView()
    {
        InitializeComponent();
        SearchTextBox.FocusAdorner = new SearchFocusAdornerTemplate();
    }

    private async void OnAddToolClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new CustomToolDialog();
        var result = await dialog.ShowDialog<CustomToolItem?>(GetOwner());
        if (result is not null && DataContext is HomeViewModel viewModel)
        {
            viewModel.AddCustomTool(result);
        }
    }

    private void OnToolContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || menu.PlacementTarget?.DataContext is not ToolCardViewModel card)
        {
            return;
        }

        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            item.DataContext = card;
        }

        var entries = menu.Items.OfType<MenuItem>().ToArray();
        if (entries.Length >= 3)
        {
            entries[0].Header = card.IsPinned ? "取消置顶" : "置顶";
            entries[1].IsVisible = card.IsCustom;
            entries[2].IsVisible = card.IsCustom;
            if (menu.Items.OfType<Separator>().FirstOrDefault() is { } separator)
            {
                separator.IsVisible = card.IsCustom;
            }
        }
    }

    private void OnTogglePinClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is ToolCardViewModel card && DataContext is HomeViewModel viewModel)
        {
            viewModel.TogglePin(card);
        }
    }

    private async void OnEditToolClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is not ToolCardViewModel { CustomTool: { } tool } || DataContext is not HomeViewModel viewModel)
        {
            return;
        }

        var dialog = new CustomToolDialog(tool);
        var result = await dialog.ShowDialog<CustomToolItem?>(GetOwner());
        if (result is not null)
        {
            viewModel.UpdateCustomTool(result);
        }
    }

    private async void OnDeleteToolClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is not ToolCardViewModel { CustomTool: { } tool } || DataContext is not HomeViewModel viewModel)
        {
            return;
        }

        var dialog = new ConfirmToolDeleteDialog(tool.Name);
        if (await dialog.ShowDialog<bool>(GetOwner()))
        {
            viewModel.DeleteCustomTool(tool.Id);
        }
    }

    private async void OnDragHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || (sender as Control)?.DataContext is not ToolCardViewModel card)
        {
            return;
        }

        e.Handled = true;
        var item = new DataTransferItem();
        item.Set(ToolCardIdFormat, card.Id);
        var transfer = new DataTransfer();
        transfer.Add(item);
        await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Move);
    }

    private void OnToolCardDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.TryGetValue(ToolCardIdFormat) is null
            ? DragDropEffects.None
            : DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnToolCardDrop(object? sender, DragEventArgs e)
    {
        var sourceId = e.DataTransfer.TryGetValue(ToolCardIdFormat);
        if (sourceId is not null && (sender as Control)?.DataContext is ToolCardViewModel target && DataContext is HomeViewModel viewModel)
        {
            viewModel.MoveTool(sourceId, target.Id);
            e.DragEffects = DragDropEffects.Move;
        }

        e.Handled = true;
    }

    private Window GetOwner() => TopLevel.GetTopLevel(this) as Window
        ?? throw new InvalidOperationException("The toolbox window is not available.");
}

internal sealed class SearchFocusAdornerTemplate : ITemplate<Control>
{
    public Control Build() => new Border
    {
        Background = Avalonia.Media.Brushes.Transparent,
        BorderBrush = Avalonia.Media.Brushes.Transparent,
        BorderThickness = new Avalonia.Thickness(0),
        IsHitTestVisible = false,
    };

    object Avalonia.Styling.ITemplate.Build() => Build();
}
