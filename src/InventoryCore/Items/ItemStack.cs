namespace InventoryCore;

public sealed class ItemStack
{
    public ItemDefinition Definition { get; }
    public int Quantity { get; private set; }
    public Guid? InstanceId { get; }

    private readonly List<AffixInstance> _affixes = new();
    private readonly List<GemSocket> _sockets;

    public bool IsUnique => InstanceId.HasValue;
    public float TotalWeight => Definition.Weight * Quantity;
    public bool IsFull => Quantity >= Definition.MaxStackSize;
    public float? CurrentDurability { get; private set; }
    public bool IsBroken => CurrentDurability.HasValue && CurrentDurability.Value <= 0f;

    public IReadOnlyList<AffixInstance> Affixes => _affixes;
    public IReadOnlyList<GemSocket> Sockets => _sockets;
    public bool HasSockets => _sockets.Count > 0;

    internal ItemStack(ItemDefinition definition, int quantity, Guid? instanceId = null)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Definition = definition;
        Quantity = quantity;
        InstanceId = instanceId;

        _sockets = instanceId.HasValue && definition.SocketCount > 0
            ? Enumerable.Range(0, definition.SocketCount).Select(i => new GemSocket(i)).ToList()
            : new List<GemSocket>();

        if (instanceId.HasValue && definition.HasDurability && definition.MaxDurability > 0)
            CurrentDurability = definition.MaxDurability;
    }

    internal bool TryAdd(int amount)
    {
        if (IsUnique || Quantity + amount > Definition.MaxStackSize) return false;
        Quantity += amount;
        return true;
    }

    internal bool TryRemove(int amount)
    {
        if (amount > Quantity) return false;
        Quantity -= amount;
        return true;
    }

    internal ItemStack Split(int amount)
    {
        if (amount >= Quantity) throw new InvalidOperationException("Cannot split entire stack.");
        Quantity -= amount;
        return new ItemStack(Definition, amount);
    }

    public void AddAffix(AffixInstance affix)
    {
        if (!IsUnique) throw new InvalidOperationException("Affixes can only be applied to unique items.");
        _affixes.Add(affix);
    }

    public OperationResult InsertGem(int socketIndex, string gemItemId)
    {
        if (!IsUnique) return OperationResult.Fail("Gems can only be inserted into unique items.");
        if (socketIndex < 0 || socketIndex >= _sockets.Count)
            return OperationResult.Fail($"Socket {socketIndex} does not exist on this item.");
        if (!_sockets[socketIndex].IsEmpty)
            return OperationResult.Fail($"Socket {socketIndex} already has a gem.");
        _sockets[socketIndex].Insert(gemItemId);
        return OperationResult.Ok();
    }

    public string? EjectGem(int socketIndex)
    {
        if (socketIndex < 0 || socketIndex >= _sockets.Count) return null;
        return _sockets[socketIndex].Eject();
    }

    public void Damage(float amount)
    {
        if (!CurrentDurability.HasValue) return;
        CurrentDurability = Math.Max(0f, CurrentDurability.Value - amount);
    }

    public void Repair(float amount)
    {
        if (!CurrentDurability.HasValue) return;
        CurrentDurability = Math.Min(Definition.MaxDurability, CurrentDurability.Value + amount);
    }

    public void FullRepair()
    {
        if (CurrentDurability.HasValue)
            CurrentDurability = Definition.MaxDurability;
    }

    public override string ToString() => IsUnique
        ? $"{Definition.DisplayName} x{Quantity} [{InstanceId!.Value:N}]"
        : $"{Definition.DisplayName} x{Quantity}";
}
