namespace InventoryCore;

public sealed class ItemRegistry
{
    private readonly Dictionary<string, ItemDefinition> _items = new();

    public IReadOnlyDictionary<string, ItemDefinition> All => _items;

    public ItemRegistry Add(ItemDefinition definition)
    {
        if (_items.ContainsKey(definition.Id))
            throw new InvalidOperationException($"Item '{definition.Id}' is already registered.");
        _items[definition.Id] = definition;
        return this;
    }

    public ItemRegistry Add(string id, string displayName, int maxStack = 1, float weight = 0f,
        string? description = null, string? iconPath = null, params string[] tags)
    {
        return Add(new ItemDefinition(id, displayName)
        {
            MaxStackSize = maxStack,
            Weight = weight,
            Description = description,
            IconPath = iconPath,
            Tags = tags
        });
    }

    public ItemDefinition Get(string id)
    {
        if (!_items.TryGetValue(id, out var def))
            throw new KeyNotFoundException($"Item '{id}' is not registered. Register it with ItemRegistry.Add() first.");
        return def;
    }

    public bool TryGet(string id, out ItemDefinition? definition) =>
        _items.TryGetValue(id, out definition);

    public bool Contains(string id) => _items.ContainsKey(id);
}
