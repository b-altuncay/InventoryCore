namespace InventoryCore;

public sealed class AffixDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public string StatKey { get; }
    public float MinValue { get; }
    public float MaxValue { get; }

    public AffixDefinition(string id, string displayName, string statKey, float minValue, float maxValue)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Affix id cannot be empty.", nameof(id));
        if (minValue > maxValue) throw new ArgumentException("MinValue must not exceed MaxValue.");
        Id = id;
        DisplayName = displayName;
        StatKey = statKey;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public override string ToString() => $"{DisplayName} ({StatKey}: {MinValue}-{MaxValue})";
}

public sealed class AffixInstance
{
    public AffixDefinition Definition { get; }
    public float Value { get; }
    public string StatKey => Definition.StatKey;
    public string DisplayName => Definition.DisplayName;

    public AffixInstance(AffixDefinition definition, float value)
    {
        Definition = definition;
        Value = value;
    }

    public override string ToString() => $"{Definition.DisplayName}: {Value:F2}";
}
