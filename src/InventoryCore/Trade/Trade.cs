namespace InventoryCore;

public static class Trade
{
    /// <summary>One-way transfer: move items from one actor to another.</summary>
    public static OperationResult Transfer(Actor from, Actor to, string itemId, int quantity = -1)
    {
        var available = from.Count(itemId);
        if (available == 0) return OperationResult.Fail($"'{from.Id}' has no '{itemId}'.");

        var qty = quantity < 0 ? available : Math.Min(quantity, available);

        var addResult = to.Add(itemId, qty);
        if (!addResult.Success) return OperationResult.Fail($"Transfer failed: {addResult.Error}");

        return from.Remove(itemId, qty);
    }

    /// <summary>
    /// Two-way barter: both actors exchange their offered items simultaneously.
    /// Fails atomically if either side cannot fulfil their offer.
    /// </summary>
    public static OperationResult Barter(
        Actor a, IEnumerable<(string itemId, int quantity)> aOffer,
        Actor b, IEnumerable<(string itemId, int quantity)> bOffer)
    {
        var aItems = aOffer.ToList();
        var bItems = bOffer.ToList();

        foreach (var (itemId, qty) in aItems)
            if (!a.Contains(itemId, qty))
                return OperationResult.Fail($"'{a.Id}' doesn't have {qty}x '{itemId}'.");

        foreach (var (itemId, qty) in bItems)
            if (!b.Contains(itemId, qty))
                return OperationResult.Fail($"'{b.Id}' doesn't have {qty}x '{itemId}'.");

        foreach (var (itemId, qty) in aItems) { a.Remove(itemId, qty); b.Add(itemId, qty); }
        foreach (var (itemId, qty) in bItems) { b.Remove(itemId, qty); a.Add(itemId, qty); }

        return OperationResult.Ok();
    }
}
