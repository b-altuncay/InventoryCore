namespace InventoryCore;

public sealed class LootEntry
{
    public string ItemId { get; }
    public int MinQuantity { get; }
    public int MaxQuantity { get; }
    public int Weight { get; }

    public LootEntry(string itemId, int minQuantity, int maxQuantity, int weight)
    {
        if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
        ItemId = itemId;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        Weight = weight;
    }
}

public sealed class LootDrop
{
    public string ItemId { get; }
    public int Quantity { get; }

    public LootDrop(string itemId, int quantity) { ItemId = itemId; Quantity = quantity; }

    public override string ToString() => $"{ItemId} x{Quantity}";
}

public sealed class LootTable
{
    public string Id { get; }
    public string DisplayName { get; }
    public int RollCount { get; init; } = 1;
    public bool AllowDuplicates { get; init; } = true;

    private readonly List<LootEntry> _entries = new();
    public IReadOnlyList<LootEntry> Entries => _entries;

    public LootTable(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public LootTable Add(string itemId, int minQty, int maxQty, int weight)
    {
        _entries.Add(new LootEntry(itemId, minQty, maxQty, weight));
        return this;
    }

    public LootTable Add(string itemId, int quantity, int weight) => Add(itemId, quantity, quantity, weight);

    private static readonly Random _defaultRng = new Random();

    public IReadOnlyList<LootDrop> Roll(Random? rng = null)
    {
        if (_entries.Count == 0) return Array.Empty<LootDrop>();
        rng ??= _defaultRng;

        var drops = new List<LootDrop>();
        var available = AllowDuplicates ? _entries.ToList() : new List<LootEntry>(_entries);
        var totalWeight = available.Sum(e => e.Weight);

        for (var i = 0; i < RollCount && available.Count > 0; i++)
        {
            var roll = rng.Next(totalWeight);
            var cumulative = 0;
            LootEntry? selected = null;
            foreach (var entry in available)
            {
                cumulative += entry.Weight;
                if (roll < cumulative) { selected = entry; break; }
            }

            if (selected is null) continue;

            var qty = selected.MinQuantity == selected.MaxQuantity
                ? selected.MinQuantity
                : rng.Next(selected.MinQuantity, selected.MaxQuantity + 1);

            drops.Add(new LootDrop(selected.ItemId, qty));

            if (!AllowDuplicates)
            {
                available.Remove(selected);
                totalWeight -= selected.Weight;
            }
        }

        return drops;
    }

    public IReadOnlyList<LootDrop> RollInto(Inventory inventory, Random? rng = null)
    {
        var drops = Roll(rng);
        foreach (var drop in drops)
            inventory.Add(drop.ItemId, drop.Quantity);
        return drops;
    }
}
