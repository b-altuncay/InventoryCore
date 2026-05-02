namespace InventoryCore.Tests;

public class CraftingTests
{
    private static (ItemRegistry registry, RecipeBook recipes, Inventory inventory) Setup()
    {
        var registry = new ItemRegistry()
            .Add("wood", "Wood", maxStack: 99)
            .Add("plank", "Plank", maxStack: 99)
            .Add("iron_ore", "Iron Ore", maxStack: 50)
            .Add("iron_sword", "Iron Sword", maxStack: 1);

        var recipes = new RecipeBook()
            .Add(new Recipe("plank_recipe", "Wood Plank")
                .Requires("wood", 2)
                .Produces("plank", 4))
            .Add(new Recipe("sword_recipe", "Iron Sword")
                .Requires("iron_ore", 3)
                .Produces("iron_sword", 1)
                .AtStation("anvil"))
            .Add(new Recipe("locked_recipe", "Secret Item")
                .Requires("wood", 1)
                .Produces("plank", 1)
                .HiddenUntil("tier2"));

        var inventory = new Inventory(registry, 30);
        return (registry, recipes, inventory);
    }

    [Fact]
    public void Craft_ShouldConsumeIngredients_AndProduceOutputs()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("wood", 10);
        var station = new CraftingStation(inv, recipes, registry);

        var result = station.Craft("plank_recipe");

        Assert.True(result.Success);
        Assert.Equal(8, inv.Count("wood"));
        Assert.Equal(4, inv.Count("plank"));
    }

    [Fact]
    public void Craft_ShouldFail_WhenNotEnoughIngredients()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("wood", 1);
        var station = new CraftingStation(inv, recipes, registry);

        var result = station.Craft("plank_recipe");

        Assert.False(result.Success);
        Assert.Equal(1, inv.Count("wood"));
    }

    [Fact]
    public void Craft_ShouldFail_WhenWrongStation()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("iron_ore", 10);
        var portable = new CraftingStation(inv, recipes, registry);

        var result = portable.Craft("sword_recipe");

        Assert.False(result.Success);
        Assert.Contains("anvil", result.Error);
    }

    [Fact]
    public void Craft_ShouldSucceed_AtCorrectStation()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("iron_ore", 6);
        var anvil = new CraftingStation("anvil", inv, recipes, registry);

        var result = anvil.Craft("sword_recipe");

        Assert.True(result.Success);
        Assert.Equal(1, inv.Count("iron_sword"));
    }

    [Fact]
    public void Craft_MultipleCount_ShouldConsumeMultiplied()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("wood", 10);
        var station = new CraftingStation(inv, recipes, registry);

        station.Craft("plank_recipe", 3);

        Assert.Equal(4, inv.Count("wood"));
        Assert.Equal(12, inv.Count("plank"));
    }

    [Fact]
    public void GetAvailableRecipes_ShouldExcludeLocked_AndWrongStation()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("wood", 10);
        inv.Add("iron_ore", 10);
        var station = new CraftingStation("anvil", inv, recipes, registry);

        var available = station.GetAvailableRecipes();

        Assert.Contains(available, r => r.Id == "plank_recipe");
        Assert.Contains(available, r => r.Id == "sword_recipe");
        Assert.DoesNotContain(available, r => r.Id == "locked_recipe");
    }

    [Fact]
    public void Unlock_ShouldMakeHiddenRecipeAvailable()
    {
        var (registry, recipes, inv) = Setup();
        inv.Add("wood", 10);
        var station = new CraftingStation(inv, recipes, registry).Unlock("tier2");

        var result = station.Craft("locked_recipe");

        Assert.True(result.Success);
    }
}
