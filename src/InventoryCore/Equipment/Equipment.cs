namespace InventoryCore;

public sealed class Equipment
{
    private readonly Dictionary<EquipSlot, ItemStack> _slots = new();

    public IReadOnlyDictionary<EquipSlot, ItemStack> Slots => _slots;
    public float TotalWeight => _slots.Values.Sum(s => s.TotalWeight);

    public ItemStack? Get(EquipSlot slot) => _slots.TryGetValue(slot, out var s) ? s : null;
    public bool IsEquipped(EquipSlot slot) => _slots.ContainsKey(slot);

    internal ItemStack? Equip(ItemStack stack)
    {
        foreach (var slot in stack.Definition.EquipSlots)
        {
            if (!_slots.ContainsKey(slot))
            {
                _slots[slot] = stack;
                return null;
            }
        }

        // All valid slots occupied — displace from primary slot
        var primary = stack.Definition.EquipSlots[0];
        var displaced = _slots[primary];
        _slots[primary] = stack;
        return displaced;
    }

    internal ItemStack? Unequip(EquipSlot slot)
    {
        if (!_slots.TryGetValue(slot, out var stack)) return null;
        _slots.Remove(slot);
        return stack;
    }
}
