namespace InventoryCore.Tests;

public class InventoryTests
{
    private static ItemRegistry BasicRegistry() =>
        new ItemRegistry()
            .Add("wood", "Wood", maxStack: 99, weight: 0.5f)
            .Add("iron_ore", "Iron Ore", maxStack: 50, weight: 2f)
            .Add("iron_sword", "Iron Sword", maxStack: 1, weight: 3.5f);

    [Fact]
    public void Add_ShouldPlaceItemInSlot()
    {
        var inv = new Inventory(BasicRegistry(), 10);
        var result = inv.Add("wood", 5);
        Assert.True(result.Success);
        Assert.Equal(5, inv.Count("wood"));
    }

    [Fact]
    public void Add_ShouldStackUpToMaxStackSize()
    {
        var inv = new Inventory(BasicRegistry(), 10);
        inv.Add("wood", 99);
        inv.Add("wood", 1);
        Assert.Equal(100, inv.Count("wood"));
        Assert.Equal(2, inv.UsedSlots);
    }

    [Fact]
    public void Remove_ShouldDeductCorrectQuantity()
    {
        var inv = new Inventory(BasicRegistry(), 10);
        inv.Add("wood", 10);
        var result = inv.Remove("wood", 4);
        Assert.True(result.Success);
        Assert.Equal(6, inv.Count("wood"));
    }

    [Fact]
    public void Remove_ShouldFail_WhenNotEnough()
    {
        var inv = new Inventory(BasicRegistry(), 10);
        inv.Add("wood", 2);
        var result = inv.Remove("wood", 5);
        Assert.False(result.Success);
        Assert.Equal(2, inv.Count("wood"));
    }

    [Fact]
    public void Add_ShouldFail_WhenNoFreeSlots()
    {
        var inv = new Inventory(BasicRegistry(), 1);
        inv.Add("iron_sword", 1);
        var result = inv.Add("iron_ore", 1);
        Assert.False(result.Success);
    }

    [Fact]
    public void TransferTo_ShouldMoveItemsBetweenInventories()
    {
        var registry = BasicRegistry();
        var player = new Inventory(registry, 10);
        var chest = new Inventory(registry, 20);
        player.Add("wood", 10);

        var result = player.TransferTo(chest, "wood", 6);

        Assert.True(result.Success);
        Assert.Equal(4, player.Count("wood"));
        Assert.Equal(6, chest.Count("wood"));
    }

    [Fact]
    public void Move_ShouldSwapSlots_WhenBothOccupied()
    {
        var registry = BasicRegistry();
        var inv = new Inventory(registry, 10);
        inv.Add("wood", 1);
        inv.Add("iron_ore", 1);
        inv.Move(0, 1);
        Assert.Equal("iron_ore", inv.Slots[0].Stack!.Definition.Id);
        Assert.Equal("wood", inv.Slots[1].Stack!.Definition.Id);
    }

    [Fact]
    public void Add_Unique_ShouldCreateInstanceId()
    {
        var inv = new Inventory(BasicRegistry(), 10);
        inv.Add("iron_sword", 1, unique: true);
        var stack = inv.GetItems().First();
        Assert.True(stack.IsUnique);
        Assert.NotNull(stack.InstanceId);
    }
}
