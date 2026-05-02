namespace InventoryCore;

public sealed class GemSocket
{
    public int Index { get; }
    public string? GemItemId { get; private set; }
    public bool IsEmpty => GemItemId is null;

    internal GemSocket(int index) { Index = index; }

    internal void Insert(string gemItemId) { GemItemId = gemItemId; }

    internal string? Eject()
    {
        var gem = GemItemId;
        GemItemId = null;
        return gem;
    }

    public override string ToString() => IsEmpty ? $"[Socket {Index}] empty" : $"[Socket {Index}] {GemItemId}";
}
