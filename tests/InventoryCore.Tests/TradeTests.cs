namespace InventoryCore.Tests;

public class TradeTests
{
    private static ItemRegistry Registry() => new ItemRegistry()
        .Add("wood", "Wood", maxStack: 99)
        .Add("gold_coin", "Gold Coin", maxStack: 999)
        .Add("iron_ore", "Iron Ore", maxStack: 50);

    [Fact]
    public void Transfer_ShouldMoveItems_BetweenActors()
    {
        var registry = Registry();
        var a = new Actor("a", registry);
        var b = new Actor("b", registry);
        a.Add("wood", 10);

        var result = Trade.Transfer(a, b, "wood", 6);

        Assert.True(result.Success);
        Assert.Equal(4, a.Count("wood"));
        Assert.Equal(6, b.Count("wood"));
    }

    [Fact]
    public void Transfer_ShouldTransferAll_WhenQuantityIsMinusOne()
    {
        var registry = Registry();
        var a = new Actor("a", registry);
        var b = new Actor("b", registry);
        a.Add("wood", 15);

        Trade.Transfer(a, b, "wood");

        Assert.Equal(0, a.Count("wood"));
        Assert.Equal(15, b.Count("wood"));
    }

    [Fact]
    public void Transfer_ShouldFail_WhenSourceHasNone()
    {
        var registry = Registry();
        var a = new Actor("a", registry);
        var b = new Actor("b", registry);

        var result = Trade.Transfer(a, b, "wood");

        Assert.False(result.Success);
    }

    [Fact]
    public void Barter_ShouldExchangeItems_BetweenActors()
    {
        var registry = Registry();
        var merchant = new Actor("merchant", registry);
        var player = new Actor("player", registry);
        merchant.Add("iron_ore", 5);
        player.Add("gold_coin", 20);

        var result = Trade.Barter(
            merchant, new[] { ("iron_ore", 5) },
            player,   new[] { ("gold_coin", 20) });

        Assert.True(result.Success);
        Assert.Equal(0, merchant.Count("iron_ore"));
        Assert.Equal(20, merchant.Count("gold_coin"));
        Assert.Equal(5, player.Count("iron_ore"));
        Assert.Equal(0, player.Count("gold_coin"));
    }

    [Fact]
    public void Barter_ShouldFail_WhenEitherSideCannotFulfil()
    {
        var registry = Registry();
        var a = new Actor("a", registry);
        var b = new Actor("b", registry);
        a.Add("wood", 3);
        b.Add("gold_coin", 5);

        var result = Trade.Barter(
            a, new[] { ("wood", 5) },   // a only has 3
            b, new[] { ("gold_coin", 5) });

        Assert.False(result.Success);
        Assert.Equal(3, a.Count("wood"));  // unchanged
        Assert.Equal(5, b.Count("gold_coin")); // unchanged
    }
}
