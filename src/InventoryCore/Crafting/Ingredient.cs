namespace InventoryCore;

public sealed class Ingredient
{
    public string ItemId { get; }
    public int Quantity { get; }

    public Ingredient(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        ItemId = itemId;
        Quantity = quantity;
    }
}

public sealed class RecipeOutput
{
    public string ItemId { get; }
    public int Quantity { get; }

    public RecipeOutput(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        ItemId = itemId;
        Quantity = quantity;
    }
}
