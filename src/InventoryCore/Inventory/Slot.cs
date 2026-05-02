namespace InventoryCore;

public sealed class Slot
{
    public int Index { get; }
    public ItemStack? Stack { get; internal set; }
    public bool IsEmpty => Stack is null;

    internal Slot(int index) { Index = index; }

    public override string ToString() => IsEmpty ? $"[{Index}] empty" : $"[{Index}] {Stack}";
}
