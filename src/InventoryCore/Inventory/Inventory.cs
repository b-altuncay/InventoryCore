namespace InventoryCore;

public sealed class Inventory
{
    private readonly ItemRegistry _registry;
    private readonly List<Slot> _slots;

    public int SlotCount => _slots.Count;
    public IReadOnlyList<Slot> Slots => _slots;
    public float TotalWeight => _slots.Where(s => !s.IsEmpty).Sum(s => s.Stack!.TotalWeight);
    public int UsedSlots => _slots.Count(s => !s.IsEmpty);
    public int FreeSlots => _slots.Count(s => s.IsEmpty);

    public Inventory(ItemRegistry registry, int slots = 30)
    {
        if (slots <= 0) throw new ArgumentOutOfRangeException(nameof(slots));
        _registry = registry;
        _slots = Enumerable.Range(0, slots).Select(i => new Slot(i)).ToList();
    }

    public OperationResult Add(string itemId, int quantity = 1, bool unique = false)
    {
        if (quantity <= 0) return OperationResult.Fail("Quantity must be positive.");
        if (!_registry.TryGet(itemId, out var def) || def is null)
            return OperationResult.Fail($"Unknown item '{itemId}'.");

        var remaining = quantity;

        if (!unique)
        {
            foreach (var slot in _slots.Where(s => !s.IsEmpty && s.Stack!.Definition.Id == itemId && !s.Stack.IsUnique && !s.Stack.IsFull))
            {
                var space = def.MaxStackSize - slot.Stack!.Quantity;
                var toAdd = Math.Min(space, remaining);
                slot.Stack.TryAdd(toAdd);
                remaining -= toAdd;
                if (remaining == 0) return OperationResult.Ok();
            }
        }

        while (remaining > 0)
        {
            var freeSlot = _slots.FirstOrDefault(s => s.IsEmpty);
            if (freeSlot is null) return OperationResult.Fail("No free slots available.");

            var instanceId = unique ? Guid.NewGuid() : (Guid?)null;
            var stackQty = unique ? 1 : Math.Min(remaining, def.MaxStackSize);
            freeSlot.Stack = new ItemStack(def, stackQty, instanceId);
            remaining -= stackQty;
        }

        return OperationResult.Ok();
    }

    internal OperationResult AddStack(ItemStack stack)
    {
        var freeSlot = _slots.FirstOrDefault(s => s.IsEmpty);
        if (freeSlot is null) return OperationResult.Fail("No free slots available.");
        freeSlot.Stack = stack;
        return OperationResult.Ok();
    }

    public OperationResult Remove(string itemId, int quantity = 1)
    {
        if (quantity <= 0) return OperationResult.Fail("Quantity must be positive.");
        if (Count(itemId) < quantity)
            return OperationResult.Fail($"Not enough '{itemId}' in inventory (have {Count(itemId)}, need {quantity}).");

        var remaining = quantity;
        foreach (var slot in _slots.Where(s => !s.IsEmpty && s.Stack!.Definition.Id == itemId).ToList())
        {
            var toRemove = Math.Min(slot.Stack!.Quantity, remaining);
            slot.Stack.TryRemove(toRemove);
            if (slot.Stack.Quantity == 0) slot.Stack = null;
            remaining -= toRemove;
            if (remaining == 0) break;
        }

        return OperationResult.Ok();
    }

    public OperationResult Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _slots.Count) return OperationResult.Fail("Source slot index out of range.");
        if (toIndex < 0 || toIndex >= _slots.Count) return OperationResult.Fail("Target slot index out of range.");

        var from = _slots[fromIndex];
        var to = _slots[toIndex];

        if (from.IsEmpty) return OperationResult.Fail("Source slot is empty.");

        if (to.IsEmpty)
        {
            to.Stack = from.Stack;
            from.Stack = null;
            return OperationResult.Ok();
        }

        if (to.Stack!.Definition.Id == from.Stack!.Definition.Id && !from.Stack.IsUnique && !to.Stack.IsUnique)
        {
            var space = to.Stack.Definition.MaxStackSize - to.Stack.Quantity;
            if (space >= from.Stack.Quantity)
            {
                to.Stack.TryAdd(from.Stack.Quantity);
                from.Stack = null;
            }
            else
            {
                to.Stack.TryAdd(space);
                from.Stack.TryRemove(space);
            }
            return OperationResult.Ok();
        }

        // Swap
        (from.Stack, to.Stack) = (to.Stack, from.Stack);
        return OperationResult.Ok();
    }

    public OperationResult TransferTo(Inventory target, string itemId, int quantity = -1)
    {
        var available = Count(itemId);
        if (available == 0) return OperationResult.Fail($"No '{itemId}' to transfer.");
        var qty = quantity < 0 ? available : Math.Min(quantity, available);

        var addResult = target.Add(itemId, qty);
        if (!addResult.Success) return addResult;

        return Remove(itemId, qty);
    }

    public int Count(string itemId) =>
        _slots.Where(s => !s.IsEmpty && s.Stack!.Definition.Id == itemId).Sum(s => s.Stack!.Quantity);

    public bool Contains(string itemId, int quantity = 1) => Count(itemId) >= quantity;

    public IEnumerable<ItemStack> GetItems() =>
        _slots.Where(s => !s.IsEmpty).Select(s => s.Stack!);

    public void Clear()
    {
        foreach (var slot in _slots) slot.Stack = null;
    }

    public override string ToString() => $"Inventory ({UsedSlots}/{SlotCount} slots used)";
}
