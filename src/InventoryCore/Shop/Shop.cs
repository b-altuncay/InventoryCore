namespace InventoryCore;

public sealed class ShopEntry
{
    public string ItemId { get; }
    public int Quantity { get; }
    public string CurrencyItemId { get; }
    public int Price { get; }
    public int? Stock { get; private set; }

    public ShopEntry(string itemId, int quantity, string currencyItemId, int price, int? stock = null)
    {
        ItemId = itemId;
        Quantity = quantity;
        CurrencyItemId = currencyItemId;
        Price = price;
        Stock = stock;
    }

    internal bool TryDecrementStock()
    {
        if (Stock is null) return true;
        if (Stock <= 0) return false;
        Stock--;
        return true;
    }
}

public sealed class Shop
{
    public string Id { get; }
    public string DisplayName { get; }

    private readonly List<ShopEntry> _entries = new();
    public IReadOnlyList<ShopEntry> Entries => _entries;

    public Shop(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public Shop Sell(string itemId, int quantity, string currencyItemId, int price, int? stock = null)
    {
        _entries.Add(new ShopEntry(itemId, quantity, currencyItemId, price, stock));
        return this;
    }

    public OperationResult Buy(string itemId, Inventory buyerInventory, int times = 1)
    {
        var entry = _entries.FirstOrDefault(e => e.ItemId == itemId);
        if (entry is null) return OperationResult.Fail($"'{itemId}' is not sold here.");

        var totalCost = entry.Price * times;
        var totalItems = entry.Quantity * times;

        if (!buyerInventory.Contains(entry.CurrencyItemId, totalCost))
            return OperationResult.Fail(
                $"Not enough {entry.CurrencyItemId} (need {totalCost}, have {buyerInventory.Count(entry.CurrencyItemId)}).");

        if (entry.Stock.HasValue && entry.Stock < times)
            return OperationResult.Fail($"Only {entry.Stock} left in stock.");

        var removeResult = buyerInventory.Remove(entry.CurrencyItemId, totalCost);
        if (!removeResult.Success) return removeResult;

        var addResult = buyerInventory.Add(entry.ItemId, totalItems);
        if (!addResult.Success)
        {
            buyerInventory.Add(entry.CurrencyItemId, totalCost);
            return addResult;
        }

        for (var i = 0; i < times; i++) entry.TryDecrementStock();

        return OperationResult.Ok();
    }
}
