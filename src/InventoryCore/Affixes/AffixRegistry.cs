namespace InventoryCore;

public sealed class AffixRegistry
{
    private readonly Dictionary<string, AffixDefinition> _affixes = new();
    private static readonly Random _rng = new Random();

    public IReadOnlyDictionary<string, AffixDefinition> All => _affixes;

    public AffixRegistry Add(AffixDefinition definition)
    {
        if (_affixes.ContainsKey(definition.Id))
            throw new InvalidOperationException($"Affix '{definition.Id}' is already registered.");
        _affixes[definition.Id] = definition;
        return this;
    }

    public AffixRegistry Add(string id, string displayName, string statKey, float minValue, float maxValue) =>
        Add(new AffixDefinition(id, displayName, statKey, minValue, maxValue));

    public AffixDefinition Get(string id)
    {
        if (!_affixes.TryGetValue(id, out var def))
            throw new KeyNotFoundException($"Affix '{id}' not registered.");
        return def;
    }

    public bool TryGet(string id, out AffixDefinition? definition) =>
        _affixes.TryGetValue(id, out definition);

    public AffixInstance Roll(string affixId, Random? rng = null)
    {
        var def = Get(affixId);
        rng ??= _rng;
        var value = def.MinValue + (float)(rng.NextDouble() * (def.MaxValue - def.MinValue));
        return new AffixInstance(def, (float)Math.Round(value, 2));
    }
}
