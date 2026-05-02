namespace InventoryCore;

public sealed class Recipe
{
    public string Id { get; }
    public string DisplayName { get; }
    public string? RequiredStation { get; private set; }
    public bool IsHiddenUntilUnlocked { get; private set; }
    public IReadOnlyList<string> RequiredUnlockKeys => _unlockKeys;

    private readonly List<Ingredient> _ingredients = new();
    private readonly List<RecipeOutput> _outputs = new();
    private readonly List<string> _unlockKeys = new();

    public IReadOnlyList<Ingredient> Ingredients => _ingredients;
    public IReadOnlyList<RecipeOutput> Outputs => _outputs;

    public Recipe(string id, string displayName)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Recipe id cannot be empty.", nameof(id));
        Id = id;
        DisplayName = displayName ?? id;
    }

    public Recipe Requires(string itemId, int quantity)
    {
        _ingredients.Add(new Ingredient(itemId, quantity));
        return this;
    }

    public Recipe Produces(string itemId, int quantity)
    {
        _outputs.Add(new RecipeOutput(itemId, quantity));
        return this;
    }

    public Recipe AtStation(string stationId)
    {
        RequiredStation = stationId;
        return this;
    }

    public Recipe HiddenUntil(params string[] unlockKeys)
    {
        IsHiddenUntilUnlocked = true;
        _unlockKeys.AddRange(unlockKeys);
        return this;
    }

    public override string ToString() => $"{DisplayName} ({Id})";
}
