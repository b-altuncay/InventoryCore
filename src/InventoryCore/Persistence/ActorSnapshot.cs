namespace InventoryCore;

public sealed class ActorSnapshot
{
    public string ActorId { get; set; } = "";
    public List<InventorySnapshot> Bags { get; set; } = new();
    public List<EquipmentSlotSnapshot> EquipmentSlots { get; set; } = new();
}

public sealed class InventorySnapshot
{
    public int SlotCount { get; set; }
    public List<SlotSnapshot> Slots { get; set; } = new();
}

public sealed class SlotSnapshot
{
    public int Index { get; set; }
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
    public string? InstanceId { get; set; }
    public float? CurrentDurability { get; set; }
    public List<AffixSnapshot> Affixes { get; set; } = new();
    public List<SocketSnapshot> Sockets { get; set; } = new();
}

public sealed class AffixSnapshot
{
    public string AffixId { get; set; } = "";
    public float Value { get; set; }
}

public sealed class SocketSnapshot
{
    public int Index { get; set; }
    public string? GemItemId { get; set; }
}

public sealed class EquipmentSlotSnapshot
{
    public string Slot { get; set; } = "";
    public SlotSnapshot? Stack { get; set; }
}
