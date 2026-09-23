using HTools.Core.Models;
using HTools.Core.Services;

namespace HTools.Core.Tests;

public class ClipboardHistoryServiceTests
{
    [Fact]
    public void Slots_StartsWithTenEmptySlotsInOrder()
    {
        var service = new ClipboardHistoryService();

        Assert.Equal(10, service.Slots.Count);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i, service.Slots[i].Index);
            Assert.Equal(string.Empty, service.Slots[i].Content);
        }
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(0, 1)]
    [InlineData(8, 9)]
    public void DisplayDigit_MapsIndexToKeyboardDigit(int index, int expectedDigit)
    {
        Assert.Equal(expectedDigit, new ClipboardSlot { Index = index }.DisplayDigit);
    }

    [Fact]
    public void AddContent_FillsFirstEmptySlot()
    {
        var service = new ClipboardHistoryService();

        service.AddContent("first");

        Assert.Equal("first", service.Slots[0].Content);
    }

    [Fact]
    public void AddContent_OverwritesLeastRecentlyUpdatedSlot_WhenAllFull()
    {
        var service = new ClipboardHistoryService();
        var slots = Enumerable.Range(0, 10)
            .Select(i => new ClipboardSlot { Index = i, Content = $"item-{i}", UpdatedAt = new DateTime(2026, 1, 1).AddMinutes(i) })
            .ToArray();
        service.LoadFrom(slots);

        service.AddContent("newest");

        // Slot 0 carries the earliest UpdatedAt of the ten, so it's the one reused.
        Assert.Equal("newest", service.GetSlot(0).Content);
    }

    [Fact]
    public void SlotsChanged_FiresOnSetAndAdd()
    {
        var service = new ClipboardHistoryService();
        var raiseCount = 0;
        service.SlotsChanged += (_, _) => raiseCount++;

        service.SetSlotContent(0, "a");
        service.AddContent("b");

        Assert.Equal(2, raiseCount);
    }

    [Fact]
    public void LoadFrom_RestoresPersistedSlotState()
    {
        var service = new ClipboardHistoryService();
        var persisted = new[] { new ClipboardSlot { Index = 5, Content = "restored", UpdatedAt = new DateTime(2026, 1, 1) } };

        service.LoadFrom(persisted);

        Assert.Equal("restored", service.GetSlot(5).Content);
    }

    [Fact]
    public void LoadFrom_RestoresHotkeyBindings()
    {
        var service = new ClipboardHistoryService();
        var persisted = new[]
        {
            new ClipboardSlot
            {
                Index = 2,
                PasteHotkey = new HotkeyDefinition { Ctrl = true, Shift = true, VirtualKey = 0x46 },
                CopyHotkey = new HotkeyDefinition { Alt = true, VirtualKey = 0x43 },
            },
        };

        service.LoadFrom(persisted);

        var slot = service.GetSlot(2);
        Assert.NotNull(slot.PasteHotkey);
        Assert.True(slot.PasteHotkey!.Ctrl);
        Assert.True(slot.PasteHotkey.Shift);
        Assert.Equal(0x46, slot.PasteHotkey.VirtualKey);
        Assert.NotNull(slot.CopyHotkey);
        Assert.True(slot.CopyHotkey!.Alt);
    }

    [Fact]
    public void SetSlotImage_SetsImagePathAndClearsText()
    {
        var service = new ClipboardHistoryService();
        service.SetSlotContent(3, "some text");

        service.SetSlotImage(3, @"C:\images\a.png");

        var slot = service.GetSlot(3);
        Assert.Equal(@"C:\images\a.png", slot.ImagePath);
        Assert.Equal(string.Empty, slot.Content);
        Assert.True(slot.IsImage);
    }

    [Fact]
    public void SetSlotContent_ClearsExistingImageAndReturnsItsPath()
    {
        var service = new ClipboardHistoryService();
        service.SetSlotImage(1, @"C:\images\old.png");

        var evicted = service.SetSlotContent(1, "text now");

        Assert.Equal(@"C:\images\old.png", evicted);
        var slot = service.GetSlot(1);
        Assert.False(slot.IsImage);
        Assert.Null(slot.ImagePath);
        Assert.Equal("text now", slot.Content);
    }

    [Fact]
    public void AddImage_FillsFirstEmptySlot()
    {
        var service = new ClipboardHistoryService();

        var evicted = service.AddImage(@"C:\images\new.png");

        Assert.Null(evicted);
        Assert.True(service.GetSlot(0).IsImage);
        Assert.Equal(@"C:\images\new.png", service.GetSlot(0).ImagePath);
    }

    [Fact]
    public void AddImage_ReturnsEvictedImagePath_WhenOverwritingImageSlot()
    {
        var service = new ClipboardHistoryService();
        var slots = Enumerable.Range(0, 10)
            .Select(i => i == 0
                ? new ClipboardSlot { Index = 0, ImagePath = @"C:\images\oldest.png", UpdatedAt = new DateTime(2026, 1, 1) }
                : new ClipboardSlot { Index = i, Content = $"item-{i}", UpdatedAt = new DateTime(2026, 1, 1).AddMinutes(i) })
            .ToArray();
        service.LoadFrom(slots);

        var evicted = service.AddImage(@"C:\images\newest.png");

        Assert.Equal(@"C:\images\oldest.png", evicted);
        Assert.Equal(@"C:\images\newest.png", service.GetSlot(0).ImagePath);
    }

    [Fact]
    public void IsEmpty_IsFalse_WhenSlotHoldsOnlyAnImage()
    {
        var slot = new ClipboardSlot { ImagePath = @"C:\images\x.png" };

        Assert.False(slot.IsEmpty);
    }

    [Fact]
    public void SetSlotHotkeys_UpdatesBindingsAndRaisesSlotsChanged()
    {
        var service = new ClipboardHistoryService();
        var raised = false;
        service.SlotsChanged += (_, _) => raised = true;
        var paste = new HotkeyDefinition { Ctrl = true, Alt = true, VirtualKey = 0x41 };

        service.SetSlotHotkeys(4, paste, null);

        Assert.True(raised);
        Assert.Same(paste, service.GetSlot(4).PasteHotkey);
        Assert.Null(service.GetSlot(4).CopyHotkey);
    }
}
