namespace InventoryCore;

public sealed class RecipeBook
{
    private readonly Dictionary<string, Recipe> _recipes = new();

    public IReadOnlyDictionary<string, Recipe> All => _recipes;

    public RecipeBook Add(Recipe recipe)
    {
        if (_recipes.ContainsKey(recipe.Id))
            throw new InvalidOperationException($"Recipe '{recipe.Id}' is already in the book.");
        _recipes[recipe.Id] = recipe;
        return this;
    }

    public Recipe Get(string id)
    {
        if (!_recipes.TryGetValue(id, out var recipe))
            throw new KeyNotFoundException($"Recipe '{id}' not found.");
        return recipe;
    }

    public bool TryGet(string id, out Recipe? recipe) => _recipes.TryGetValue(id, out recipe);

    public IEnumerable<Recipe> GetByStation(string? stationId) =>
        _recipes.Values.Where(r => r.RequiredStation == stationId);
}
