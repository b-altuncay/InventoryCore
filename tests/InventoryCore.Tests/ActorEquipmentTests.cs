namespace InventoryCore.Tests;

public class ActorEquipmentTests
{
    private static ItemRegistry Registry() => new ItemRegistry()
        .Add("wood", "Wood", maxStack: 99)
        .Add("iron_ore", "Iron Ore", maxStack: 50)
        .Add("iron_sword", new ItemDefinition("iron_sword", "Iron Sword")
        {
            MaxStackSize = 1, Weight = 3.5f, Rarity = ItemRarity.Rare,
            EquipSlots = new[] { EquipSlot.MainHand }, SocketCount = 2,
            HasDurability = true, MaxDurability = 100f
        })
        .Add("shield", new ItemDefinition("shield", "Shield")
        {
            MaxStackSize = 1, EquipSlots = new[] { EquipSlot.OffHand }
        })
        .Add("ruby", "Ruby", maxStack: 10)
        .Add("gold_coin", "Gold Coin", maxStack: 999);

    private static ItemRegistry Registry(params (string id, ItemDefinition def)[] _) => Registry();

    [Fact]
    public void Actor_Add_ShouldWorkAcrossBags()
    {
        var actor = new Actor("player", Registry(), bagCount: 2, slotsPerBag: 2);
        for (var i = 0; i < 4; i++) actor.Add("iron_ore", 1);
        Assert.Equal(4, actor.Count("iron_ore"));
    }

    [Fact]
    public void Actor_Remove_ShouldRemoveAcrossBags()
    {
        var actor = new Actor("player", Registry(), bagCount: 2, slotsPerBag: 5);
        actor.Add("wood", 99);
        actor.Add("wood", 99);
        actor.Remove("wood", 150);
        Assert.Equal(48, actor.Count("wood"));
    }

    [Fact]
    public void Actor_Equip_ShouldMoveItemFromBagToEquipment()
    {
        var actor = new Actor("player", Registry());
        actor.Add("iron_sword");

        var result = actor.Equip("iron_sword");

        Assert.True(result.Success);
        Assert.Equal(0, actor.Count("iron_sword"));
        Assert.NotNull(actor.Equipment.Get(EquipSlot.MainHand));
    }

    [Fact]
    public void Actor_Equip_ShouldDisplace_WhenSlotOccupied()
    {
        var actor = new Actor("player", Registry(), slotsPerBag: 10);
        actor.Add("iron_sword");
        actor.Equip("iron_sword");
        actor.Add("iron_sword"); // second sword

        var result = actor.Equip("iron_sword");

        Assert.True(result.Success);
        Assert.Equal(1, actor.Count("iron_sword")); // displaced sword returned to bag
    }

    [Fact]
    public void Actor_Unequip_ShouldReturnItemToBag()
    {
        var actor = new Actor("player", Registry(), slotsPerBag: 10);
        actor.Add("iron_sword");
        actor.Equip("iron_sword");

        var result = actor.Unequip(EquipSlot.MainHand);

        Assert.True(result.Success);
        Assert.Equal(1, actor.Count("iron_sword"));
        Assert.Null(actor.Equipment.Get(EquipSlot.MainHand));
    }

    [Fact]
    public void Actor_InsertGem_ShouldSocketGemIntoUniqueItem()
    {
        var actor = new Actor("player", Registry(), slotsPerBag: 10);
        actor.Add("iron_sword", unique: true);
        actor.Add("ruby", 3);

        var sword = actor.GetAllItems().First(s => s.Definition.Id == "iron_sword");
        var result = actor.InsertGem(sword.InstanceId!.Value, socketIndex: 0, "ruby");

        Assert.True(result.Success);
        Assert.Equal(2, actor.Count("ruby")); // one ruby consumed
        Assert.Equal("ruby", sword.Sockets[0].GemItemId);
    }

    [Fact]
    public void Actor_EjectGem_ShouldReturnGemToBag()
    {
        var actor = new Actor("player", Registry(), slotsPerBag: 10);
        actor.Add("iron_sword", unique: true);
        actor.Add("ruby", 1);
        var sword = actor.GetAllItems().First(s => s.Definition.Id == "iron_sword");
        actor.InsertGem(sword.InstanceId!.Value, 0, "ruby");

        var result = actor.EjectGem(sword.InstanceId!.Value, 0);

        Assert.True(result.Success);
        Assert.Equal(1, actor.Count("ruby"));
        Assert.True(sword.Sockets[0].IsEmpty);
    }

    [Fact]
    public void ItemStack_Durability_ShouldTrackDamageAndRepair()
    {
        var registry = Registry();
        var inv = new Inventory(registry, 5);
        inv.Add("iron_sword", unique: true);
        var sword = inv.GetItems().First();

        Assert.Equal(100f, sword.CurrentDurability);
        sword.Damage(30f);
        Assert.Equal(70f, sword.CurrentDurability);
        sword.Repair(10f);
        Assert.Equal(80f, sword.CurrentDurability);
        sword.FullRepair();
        Assert.Equal(100f, sword.CurrentDurability);
    }

    [Fact]
    public void Actor_Snapshot_ShouldPreserveAffixesAndSockets()
    {
        var registry = Registry();
        var affixRegistry = new AffixRegistry()
            .Add("fire_dmg", "of Flame", "fire_damage", 5f, 20f);

        var actor = new Actor("hero", registry, slotsPerBag: 10);
        actor.Add("iron_sword", unique: true);
        var sword = actor.GetAllItems().First(s => s.Definition.Id == "iron_sword");

        sword.AddAffix(new AffixInstance(affixRegistry.Get("fire_dmg"), 15f));
        actor.Add("ruby");
        actor.InsertGem(sword.InstanceId!.Value, 0, "ruby");

        var snapshot = actor.TakeSnapshot();
        var restored = Actor.Restore(snapshot, registry, affixRegistry);

        var restoredSword = restored.GetAllItems().First(s => s.Definition.Id == "iron_sword");
        Assert.Single(restoredSword.Affixes);
        Assert.Equal(15f, restoredSword.Affixes[0].Value);
        Assert.Equal("ruby", restoredSword.Sockets[0].GemItemId);
    }
}

// Helper extension on ItemRegistry for test setup
file static class TestExtensions
{
    public static ItemRegistry Add(this ItemRegistry reg, string id, ItemDefinition def) => reg.Add(def);
}
