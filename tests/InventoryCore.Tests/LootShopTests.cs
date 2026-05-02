namespace InventoryCore.Tests;

public class LootShopTests
{
    private static ItemRegistry Registry() =>
        new ItemRegistry()
            .Add("iron_ore", "Iron Ore", maxStack: 50)
            .Add("rare_gem", "Rare Gem", maxStack: 10)
            .Add("iron_sword", "Iron Sword", maxStack: 1)
            .Add("gold_coin", "Gold Coin", maxStack: 999);

    [Fact]
    public void LootTable_Roll_ShouldReturnExpectedCount()
    {
        var table = new LootTable("chest", "Treasure Chest") { RollCount = 3 }
            .Add("iron_ore", 1, 3, weight: 90)
            .Add("rare_gem", 1, 1, weight: 10);

        var drops = table.Roll(new Random(42));

        Assert.Equal(3, drops.Count);
    }

    [Fact]
    public void LootTable_RollInto_ShouldAddItemsToInventory()
    {
        var registry = Registry();
        var inv = new Inventory(registry, 20);
        var table = new LootTable("chest", "Chest") { RollCount = 2 }
            .Add("iron_ore", 1, weight: 100);

        table.RollInto(inv);

        Assert.True(inv.Count("iron_ore") > 0);
    }

    [Fact]
    public void LootTable_AllowDuplicates_False_ShouldNotRepeat()
    {
        var table = new LootTable("unique_chest", "Unique Chest") { RollCount = 2, AllowDuplicates = false }
            .Add("iron_ore", 1, weight: 50)
            .Add("rare_gem", 1, weight: 50);

        var drops = table.Roll(new Random(1));

        var ids = drops.Select(d => d.ItemId).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void Shop_Buy_ShouldDeductCurrency_AndAddItem()
    {
        var registry = Registry();
        var inv = new Inventory(registry, 20);
        inv.Add("gold_coin", 100);

        var shop = new Shop("blacksmith", "Blacksmith")
            .Sell("iron_sword", 1, "gold_coin", price: 50);

        var result = shop.Buy("iron_sword", inv);

        Assert.True(result.Success);
        Assert.Equal(1, inv.Count("iron_sword"));
        Assert.Equal(50, inv.Count("gold_coin"));
    }

    [Fact]
    public void Shop_Buy_ShouldFail_WhenNotEnoughCurrency()
    {
        var registry = Registry();
        var inv = new Inventory(registry, 20);
        inv.Add("gold_coin", 10);

        var shop = new Shop("blacksmith", "Blacksmith")
            .Sell("iron_sword", 1, "gold_coin", price: 50);

        var result = shop.Buy("iron_sword", inv);

        Assert.False(result.Success);
        Assert.Equal(10, inv.Count("gold_coin"));
    }

    [Fact]
    public void Shop_Buy_ShouldRespectStock()
    {
        var registry = Registry();
        var inv = new Inventory(registry, 20);
        inv.Add("gold_coin", 999);

        var shop = new Shop("limited", "Limited Shop")
            .Sell("iron_sword", 1, "gold_coin", price: 10, stock: 1);

        shop.Buy("iron_sword", inv);
        var second = shop.Buy("iron_sword", inv);

        Assert.False(second.Success);
        Assert.Equal(1, inv.Count("iron_sword"));
    }
}
