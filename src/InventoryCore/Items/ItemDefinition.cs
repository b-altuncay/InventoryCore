namespace InventoryCore;

public sealed class ItemDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public int MaxStackSize { get; init; } = 1;
    public float Weight { get; init; } = 0f;
    public string? Description { get; init; }
    public string? IconPath { get; init; }
    public ItemRarity Rarity { get; init; } = ItemRarity.Common;
    public bool HasDurability { get; init; } = false;
    public float MaxDurability { get; init; } = 0f;
    public int SocketCount { get; init; } = 0;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<EquipSlot> EquipSlots { get; init; } = Array.Empty<EquipSlot>();

    public bool IsEquippable => EquipSlots.Count > 0;

    public ItemDefinition(string id, string displayName)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Item id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        Id = id;
        DisplayName = displayName;
    }

    public bool HasTag(string tag) => Tags.Contains(tag);

    public override string ToString() => $"{DisplayName} [{Rarity}] ({Id})";
}
