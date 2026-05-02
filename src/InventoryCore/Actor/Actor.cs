namespace InventoryCore;

public sealed class Actor
{
    public string Id { get; }
    public IReadOnlyList<Inventory> Bags { get; }
    public Equipment Equipment { get; }

    public Inventory PrimaryBag => Bags[0];
    public float TotalWeight => Bags.Sum(b => b.TotalWeight) + Equipment.TotalWeight;

    public Actor(string id, ItemRegistry registry, int bagCount = 1, int slotsPerBag = 30)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Actor id cannot be empty.", nameof(id));
        if (bagCount <= 0) throw new ArgumentOutOfRangeException(nameof(bagCount));
        Id = id;
        Bags = Enumerable.Range(0, bagCount).Select(_ => new Inventory(registry, slotsPerBag)).ToList();
        Equipment = new Equipment();
    }

    // ── Inventory access ────────────────────────────────────────────────────

    public int Count(string itemId) => Bags.Sum(b => b.Count(itemId));
    public bool Contains(string itemId, int quantity = 1) => Count(itemId) >= quantity;

    public OperationResult Add(string itemId, int quantity = 1, bool unique = false)
    {
        foreach (var bag in Bags)
        {
            if (bag.FreeSlots > 0)
                return bag.Add(itemId, quantity, unique);
        }
        return OperationResult.Fail("All bags are full.");
    }

    public OperationResult Remove(string itemId, int quantity = 1)
    {
        var remaining = quantity;
        foreach (var bag in Bags)
        {
            var available = bag.Count(itemId);
            if (available == 0) continue;
            var toRemove = Math.Min(available, remaining);
            bag.Remove(itemId, toRemove);
            remaining -= toRemove;
            if (remaining == 0) return OperationResult.Ok();
        }
        return remaining > 0
            ? OperationResult.Fail($"Not enough '{itemId}' (needed {quantity}, could only remove {quantity - remaining}).")
            : OperationResult.Ok();
    }

    public IEnumerable<ItemStack> GetAllItems() => Bags.SelectMany(b => b.GetItems());

    public ItemStack? FindByInstanceId(Guid instanceId)
    {
        foreach (var bag in Bags)
            foreach (var slot in bag.Slots)
                if (slot.Stack?.InstanceId == instanceId)
                    return slot.Stack;
        return null;
    }

    // ── Equipment ───────────────────────────────────────────────────────────

    public OperationResult Equip(string itemId)
    {
        foreach (var bag in Bags)
            foreach (var slot in bag.Slots)
                if (slot.Stack?.Definition.Id == itemId)
                    return EquipFromSlot(bag, slot);

        return OperationResult.Fail($"'{itemId}' not found in any bag.");
    }

    public OperationResult EquipByInstanceId(Guid instanceId)
    {
        foreach (var bag in Bags)
            foreach (var slot in bag.Slots)
                if (slot.Stack?.InstanceId == instanceId)
                    return EquipFromSlot(bag, slot);

        return OperationResult.Fail("Item not found.");
    }

    private OperationResult EquipFromSlot(Inventory bag, Slot slot)
    {
        var stack = slot.Stack!;
        if (!stack.Definition.IsEquippable)
            return OperationResult.Fail($"'{stack.Definition.DisplayName}' has no equip slots defined.");

        var displaced = Equipment.Equip(stack);
        slot.Stack = null;

        if (displaced != null)
            bag.AddStack(displaced);

        return OperationResult.Ok();
    }

    public OperationResult Unequip(EquipSlot slot)
    {
        var stack = Equipment.Unequip(slot);
        if (stack is null) return OperationResult.Fail($"Nothing equipped in {slot}.");

        foreach (var bag in Bags)
        {
            if (bag.AddStack(stack).Success) return OperationResult.Ok();
        }

        Equipment.Equip(stack); // put back on failure
        return OperationResult.Fail("All bags are full.");
    }

    // ── Gems / Sockets ──────────────────────────────────────────────────────

    public OperationResult InsertGem(Guid targetInstanceId, int socketIndex, string gemItemId)
    {
        var target = FindByInstanceId(targetInstanceId);
        if (target is null) return OperationResult.Fail("Target item not found.");
        if (!Contains(gemItemId)) return OperationResult.Fail($"'{gemItemId}' not in bags.");

        var result = target.InsertGem(socketIndex, gemItemId);
        if (!result.Success) return result;

        Remove(gemItemId, 1);
        return OperationResult.Ok();
    }

    public OperationResult EjectGem(Guid targetInstanceId, int socketIndex)
    {
        var target = FindByInstanceId(targetInstanceId);
        if (target is null) return OperationResult.Fail("Target item not found.");

        var gemId = target.EjectGem(socketIndex);
        if (gemId is null) return OperationResult.Fail($"Socket {socketIndex} is already empty.");

        Add(gemId, 1);
        return OperationResult.Ok();
    }

    // ── Snapshots / Persistence ─────────────────────────────────────────────

    public ActorSnapshot TakeSnapshot()
    {
        var snapshot = new ActorSnapshot { ActorId = Id };

        foreach (var bag in Bags)
        {
            var inv = new InventorySnapshot { SlotCount = bag.SlotCount };
            foreach (var slot in bag.Slots.Where(s => !s.IsEmpty))
            {
                var stack = slot.Stack!;
                var slotSnap = new SlotSnapshot
                {
                    Index = slot.Index,
                    ItemId = stack.Definition.Id,
                    Quantity = stack.Quantity,
                    InstanceId = stack.InstanceId?.ToString(),
                    CurrentDurability = stack.CurrentDurability
                };
                foreach (var affix in stack.Affixes)
                    slotSnap.Affixes.Add(new AffixSnapshot { AffixId = affix.Definition.Id, Value = affix.Value });
                foreach (var socket in stack.Sockets)
                    slotSnap.Sockets.Add(new SocketSnapshot { Index = socket.Index, GemItemId = socket.GemItemId });
                inv.Slots.Add(slotSnap);
            }
            snapshot.Bags.Add(inv);
        }

        foreach (var (slot, stack) in Equipment.Slots)
        {
            var slotSnap = new SlotSnapshot
            {
                Index = 0,
                ItemId = stack.Definition.Id,
                Quantity = stack.Quantity,
                InstanceId = stack.InstanceId?.ToString(),
                CurrentDurability = stack.CurrentDurability
            };
            foreach (var affix in stack.Affixes)
                slotSnap.Affixes.Add(new AffixSnapshot { AffixId = affix.Definition.Id, Value = affix.Value });
            foreach (var s in stack.Sockets)
                slotSnap.Sockets.Add(new SocketSnapshot { Index = s.Index, GemItemId = s.GemItemId });
            snapshot.EquipmentSlots.Add(new EquipmentSlotSnapshot { Slot = slot.ToString(), Stack = slotSnap });
        }

        return snapshot;
    }

    public static Actor Restore(ActorSnapshot snapshot, ItemRegistry registry, AffixRegistry? affixRegistry = null)
    {
        var actor = new Actor(snapshot.ActorId, registry,
            bagCount: snapshot.Bags.Count,
            slotsPerBag: snapshot.Bags.Count > 0 ? snapshot.Bags[0].SlotCount : 30);

        for (var i = 0; i < snapshot.Bags.Count && i < actor.Bags.Count; i++)
        {
            var bag = actor.Bags[i];
            foreach (var slotSnap in snapshot.Bags[i].Slots)
                RestoreStack(slotSnap, bag.Slots[slotSnap.Index], registry, affixRegistry);
        }

        foreach (var equipSnap in snapshot.EquipmentSlots)
        {
            if (equipSnap.Stack is null) continue;
            if (!registry.TryGet(equipSnap.Stack.ItemId, out var def) || def is null) continue;
            var instanceId = equipSnap.Stack.InstanceId != null ? Guid.Parse(equipSnap.Stack.InstanceId) : (Guid?)null;
            var stack = new ItemStack(def, equipSnap.Stack.Quantity, instanceId);
            RestoreAffixesAndSockets(equipSnap.Stack, stack, affixRegistry);
            if (Enum.TryParse<EquipSlot>(equipSnap.Slot, out var slot))
                actor.Equipment.Equip(stack);
        }

        return actor;
    }

    private static void RestoreStack(SlotSnapshot snap, Slot slot, ItemRegistry registry, AffixRegistry? affixes)
    {
        if (!registry.TryGet(snap.ItemId, out var def) || def is null) return;
        var instanceId = snap.InstanceId != null ? Guid.Parse(snap.InstanceId) : (Guid?)null;
        var stack = new ItemStack(def, snap.Quantity, instanceId);
        if (snap.CurrentDurability.HasValue && stack.CurrentDurability.HasValue)
            RestoreDurability(stack, snap.CurrentDurability.Value);
        RestoreAffixesAndSockets(snap, stack, affixes);
        slot.Stack = stack;
    }

    private static void RestoreAffixesAndSockets(SlotSnapshot snap, ItemStack stack, AffixRegistry? affixes)
    {
        if (affixes != null)
            foreach (var a in snap.Affixes)
                if (affixes.TryGet(a.AffixId, out var def) && def != null)
                    stack.AddAffix(new AffixInstance(def, a.Value));

        foreach (var s in snap.Sockets)
            if (s.GemItemId != null)
                stack.InsertGem(s.Index, s.GemItemId);
    }

    private static void RestoreDurability(ItemStack stack, float value)
    {
        var dmg = stack.Definition.MaxDurability - value;
        if (dmg > 0) stack.Damage(dmg);
    }

    public override string ToString() => $"Actor({Id}, {Bags.Count} bag(s), {Count(""):0}w)";
}
