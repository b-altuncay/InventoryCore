using InventoryCore;

// ── 1. Registries ────────────────────────────────────────────────────────────
var registry = new ItemRegistry()
    .Add("wood",       "Wood",       maxStack: 99, weight: 0.5f)
    .Add("plank",      "Plank",      maxStack: 99, weight: 0.3f)
    .Add("gold_coin",  "Gold Coin",  maxStack: 999)
    .Add(new ItemDefinition("iron_ore", "Iron Ore") { MaxStackSize = 50, Weight = 2f })
    .Add(new ItemDefinition("iron_sword", "Iron Sword")
    {
        MaxStackSize = 1, Weight = 3.5f, Rarity = ItemRarity.Rare,
        EquipSlots  = new[] { EquipSlot.MainHand },
        SocketCount = 2, HasDurability = true, MaxDurability = 100f
    })
    .Add(new ItemDefinition("ruby", "Ruby") { MaxStackSize = 10, Tags = new[] { "gem" } });

var affixRegistry = new AffixRegistry()
    .Add("fire_dmg",  "of Flame",    statKey: "fire_damage",  minValue: 5f,  maxValue: 20f)
    .Add("crit_rate", "of Precision",statKey: "crit_chance",  minValue: 1f,  maxValue: 5f);

var recipes = new RecipeBook()
    .Add(new Recipe("plank_recipe", "Wood Plank").Requires("wood", 2).Produces("plank", 4))
    .Add(new Recipe("sword_recipe", "Iron Sword")
        .Requires("iron_ore", 3).Produces("iron_sword", 1).AtStation("anvil"));

// ── 2. Actor ─────────────────────────────────────────────────────────────────
var player = new Actor("player-1", registry, bagCount: 2, slotsPerBag: 20);
player.Add("wood", 10);
player.Add("iron_ore", 6);
player.Add("gold_coin", 100);
player.Add("ruby", 3);
Console.WriteLine($"Created: {player.Id}, {player.Bags.Count} bags");

// ── 3. Crafting ───────────────────────────────────────────────────────────────
var campfire = new CraftingStation(player.PrimaryBag, recipes, registry);
campfire.Craft("plank_recipe", 2);
Console.WriteLine($"Crafted planks | Wood: {player.Count("wood")}  Planks: {player.Count("plank")}");

var anvil = new CraftingStation("anvil", player.PrimaryBag, recipes, registry);
anvil.Craft("sword_recipe");
Console.WriteLine($"Crafted sword  | Iron Ore: {player.Count("iron_ore")}  Swords: {player.Count("iron_sword")}");

// ── 4. Equipment ──────────────────────────────────────────────────────────────
// Grant a unique drop sword (unique = instance ID, supports affixes + sockets)
player.Add("iron_sword", unique: true);
var dropSword = player.GetAllItems().First(s => s.Definition.Id == "iron_sword" && s.IsUnique);
player.EquipByInstanceId(dropSword.InstanceId!.Value);
var equipped = player.Equipment.Get(EquipSlot.MainHand)!;
Console.WriteLine($"Equipped: {equipped.Definition.DisplayName} [{equipped.Definition.Rarity}] (unique: {equipped.IsUnique})");

// ── 5. Affixes ────────────────────────────────────────────────────────────────
equipped.AddAffix(affixRegistry.Roll("fire_dmg"));
equipped.AddAffix(affixRegistry.Roll("crit_rate"));
Console.WriteLine("Affixes:");
foreach (var a in equipped.Affixes)
    Console.WriteLine($"  {a}");

// ── 6. Sockets ────────────────────────────────────────────────────────────────
var swordId = equipped.InstanceId!.Value;
player.Unequip(EquipSlot.MainHand); // return to bag before socketing
var swordStack = player.FindByInstanceId(swordId)!;
player.InsertGem(swordId, socketIndex: 0, "ruby");
player.InsertGem(swordId, socketIndex: 1, "ruby");
Console.WriteLine($"Sockets: {string.Join(", ", swordStack.Sockets.Select(s => s.GemItemId ?? "empty"))}");
Console.WriteLine($"Rubies left: {player.Count("ruby")}");

// ── 7. Durability ─────────────────────────────────────────────────────────────
swordStack.Damage(35f);
Console.WriteLine($"Durability after combat: {swordStack.CurrentDurability}/100");
swordStack.FullRepair();
Console.WriteLine($"After repair: {swordStack.CurrentDurability}/100");

// ── 8. Transfer to chest ─────────────────────────────────────────────────────
var chest = new Actor("chest-1", registry, slotsPerBag: 50);
Trade.Transfer(player, chest, "plank");
Console.WriteLine($"Planks sent to chest: {chest.Count("plank")}");

// ── 9. Barter ─────────────────────────────────────────────────────────────────
var merchant = new Actor("merchant", registry);
merchant.Add("iron_ore", 10);
Trade.Barter(
    player,   new[] { ("gold_coin", 30) },
    merchant, new[] { ("iron_ore",  5)  });
Console.WriteLine($"Traded 30 gold for 5 iron ore | Gold: {player.Count("gold_coin")}  Ore: {player.Count("iron_ore")}");

// ── 10. Loot ──────────────────────────────────────────────────────────────────
var loot = new LootTable("goblin", "Goblin Drop") { RollCount = 3 }
    .Add("gold_coin", minQty: 5, maxQty: 15, weight: 70)
    .Add("iron_ore",  minQty: 1, maxQty: 3,  weight: 25)
    .Add("ruby",      quantity: 1,            weight: 5);
var drops = loot.RollInto(player.PrimaryBag);
Console.WriteLine($"Loot: {string.Join(", ", drops)}");

// ── 11. Shop ──────────────────────────────────────────────────────────────────
var shop = new Shop("blacksmith", "Blacksmith")
    .Sell("iron_ore", quantity: 5, currencyItemId: "gold_coin", price: 10);
shop.Buy("iron_ore", player.PrimaryBag);
Console.WriteLine($"Bought ore from shop | Gold: {player.Count("gold_coin")}");

// ── 12. Snapshot / Persistence ────────────────────────────────────────────────
var store  = new InMemoryActorStore();
var snap   = player.TakeSnapshot();
store.Save(snap);

var restored = Actor.Restore(store.Load("player-1")!, registry, affixRegistry);
var rSword   = restored.FindByInstanceId(swordId);
Console.WriteLine($"\nRestored actor has {restored.Count("iron_ore")} iron ore");
Console.WriteLine($"Restored sword affixes: {rSword?.Affixes.Count ?? 0}");
Console.WriteLine($"Restored sword socket 0: {rSword?.Sockets[0].GemItemId}");
