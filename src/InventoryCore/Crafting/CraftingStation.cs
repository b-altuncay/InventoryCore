namespace InventoryCore;

public sealed class CraftingStation
{
    public string? Id { get; }
    private readonly Inventory _inventory;
    private readonly RecipeBook _recipes;
    private readonly ItemRegistry _registry;
    private readonly HashSet<string> _unlockedKeys = new();

    /// <summary>Portable crafting (no station required).</summary>
    public CraftingStation(Inventory inventory, RecipeBook recipes, ItemRegistry registry)
        : this(null, inventory, recipes, registry) { }

    public CraftingStation(string? id, Inventory inventory, RecipeBook recipes, ItemRegistry registry)
    {
        Id = id;
        _inventory = inventory;
        _recipes = recipes;
        _registry = registry;
    }

    public CraftingStation Unlock(params string[] keys)
    {
        foreach (var k in keys) _unlockedKeys.Add(k);
        return this;
    }

    public bool CanCraft(string recipeId, int count = 1)
    {
        if (!_recipes.TryGet(recipeId, out var recipe) || recipe is null) return false;
        if (recipe.RequiredStation != null && recipe.RequiredStation != Id) return false;
        if (recipe.IsHiddenUntilUnlocked && !recipe.RequiredUnlockKeys.All(k => _unlockedKeys.Contains(k))) return false;
        return recipe.Ingredients.All(ing => _inventory.Contains(ing.ItemId, ing.Quantity * count));
    }

    public IReadOnlyList<Recipe> GetAvailableRecipes()
    {
        return _recipes.All.Values
            .Where(r =>
                (r.RequiredStation == null || r.RequiredStation == Id) &&
                (!r.IsHiddenUntilUnlocked || r.RequiredUnlockKeys.All(k => _unlockedKeys.Contains(k))) &&
                r.Ingredients.All(ing => _inventory.Contains(ing.ItemId, ing.Quantity)))
            .ToList();
    }

    public OperationResult<IReadOnlyList<CraftedItem>> Craft(string recipeId, int count = 1)
    {
        if (count <= 0) return OperationResult<IReadOnlyList<CraftedItem>>.Fail("Count must be positive.");

        if (!_recipes.TryGet(recipeId, out var recipe) || recipe is null)
            return OperationResult<IReadOnlyList<CraftedItem>>.Fail($"Recipe '{recipeId}' not found.");

        if (recipe.RequiredStation != null && recipe.RequiredStation != Id)
            return OperationResult<IReadOnlyList<CraftedItem>>.Fail(
                $"This recipe requires station '{recipe.RequiredStation}'.");

        if (recipe.IsHiddenUntilUnlocked && !recipe.RequiredUnlockKeys.All(k => _unlockedKeys.Contains(k)))
            return OperationResult<IReadOnlyList<CraftedItem>>.Fail("Recipe is locked.");

        foreach (var ing in recipe.Ingredients)
        {
            if (!_inventory.Contains(ing.ItemId, ing.Quantity * count))
                return OperationResult<IReadOnlyList<CraftedItem>>.Fail(
                    $"Not enough '{ing.ItemId}' (need {ing.Quantity * count}, have {_inventory.Count(ing.ItemId)}).");
        }

        foreach (var ing in recipe.Ingredients)
            _inventory.Remove(ing.ItemId, ing.Quantity * count);

        var crafted = new List<CraftedItem>();
        foreach (var output in recipe.Outputs)
        {
            var totalQty = output.Quantity * count;
            _inventory.Add(output.ItemId, totalQty);
            crafted.Add(new CraftedItem(output.ItemId, totalQty));
        }

        return OperationResult<IReadOnlyList<CraftedItem>>.Ok(crafted);
    }
}

public sealed class CraftedItem
{
    public string ItemId { get; }
    public int Quantity { get; }

    public CraftedItem(string itemId, int quantity) { ItemId = itemId; Quantity = quantity; }

    public override string ToString() => $"{ItemId} x{Quantity}";
}
