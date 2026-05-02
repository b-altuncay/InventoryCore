# InventoryCore

A lightweight, framework-agnostic inventory and crafting library for games.  
Works in **Godot**, **Unity**, **MonoGame**, or any .NET project.

```
dotnet add package InventoryCore
```

---

## Copy. Paste. Run.

```csharp
using InventoryCore;

// 1. Register items
var registry = new ItemRegistry()
    .Add("wood",       "Wood",       maxStack: 99, weight: 0.5f)
    .Add("plank",      "Plank",      maxStack: 99, weight: 0.3f)
    .Add("iron_ore",   "Iron Ore",   maxStack: 50, weight: 2.0f)
    .Add("iron_sword", "Iron Sword", maxStack: 1,  weight: 3.5f)
    .Add("gold_coin",  "Gold Coin",  maxStack: 999);

// 2. Define recipes
var recipes = new RecipeBook()
    .Add(new Recipe("plank_recipe", "Wood Plank")
        .Requires("wood", 2)
        .Produces("plank", 4))
    .Add(new Recipe("sword_recipe", "Iron Sword")
        .Requires("iron_ore", 3)
        .Produces("iron_sword", 1)
        .AtStation("anvil"));

// 3. Create an inventory
var player = new Inventory(registry, slots: 30);
player.Add("wood", 10);
player.Add("iron_ore", 6);

// 4. Craft anywhere
var result = new CraftingStation(player, recipes, registry).Craft("plank_recipe");
// result.Success == true  |  player now has 8 planks, 8 wood

// 5. Craft at a specific station
var anvil = new CraftingStation("anvil", player, recipes, registry);
anvil.Craft("sword_recipe");
// player now has 1 iron_sword, 3 iron_ore

// 6. Send everything to a chest
var chest = new Inventory(registry, slots: 50);
player.TransferTo(chest, "plank");
// one line. done.
```

---

## Features

| Feature | Description |
|---|---|
| **Inventory** | Slot-based, configurable size, stacking, move, swap |
| **Crafting** | Station-aware, multi-output, batch craft, recipe unlock |
| **Loot Tables** | Weighted random drops, roll count, no-duplicate mode |
| **Shops** | Currency-based purchases, stock limits |
| **Unique Items** | Instance IDs for socketable / affix-ready items |
| **Transfer** | Move items between any two inventories in one call |

---

## Crafting

```csharp
var station = new CraftingStation("anvil", inventory, recipes, registry);

// What can we craft right now?
var available = station.GetAvailableRecipes();

// Craft 3 at once
var result = station.Craft("plank_recipe", count: 3);
if (!result.Success)
    Console.WriteLine(result.Error); // "Not enough 'wood' (need 6, have 2)."

// Locked recipes (unlock via progression system)
var recipe = new Recipe("dragon_armor", "Dragon Armor")
    .Requires("dragon_scale", 5)
    .Produces("dragon_armor", 1)
    .HiddenUntil("chapter3_complete");

station.Unlock("chapter3_complete"); // recipe becomes available
```

---

## Loot Tables

```csharp
var chest = new LootTable("forest_chest", "Forest Chest") { RollCount = 3 }
    .Add("iron_ore",  minQty: 1, maxQty: 3, weight: 70)
    .Add("gold_coin", minQty: 5, maxQty: 15, weight: 25)
    .Add("rare_gem",  quantity: 1,           weight: 5);

// Just roll
var drops = chest.Roll();

// Roll and automatically add to inventory
chest.RollInto(playerInventory);
```

---

## Shops

```csharp
var shop = new Shop("blacksmith", "Blacksmith")
    .Sell("iron_sword", quantity: 1, currencyItemId: "gold_coin", price: 50)
    .Sell("iron_ore",   quantity: 5, currencyItemId: "gold_coin", price: 10, stock: 20);

var result = shop.Buy("iron_sword", playerInventory);
// Deducts gold, adds sword. Fails gracefully if not enough gold or out of stock.
```

---

## OperationResult

All mutating operations return an `OperationResult` -- no exceptions for expected failures.

```csharp
var result = inventory.Add("unknown_item", 5);
if (!result.Success)
    Console.WriteLine(result.Error); // "Unknown item 'unknown_item'."

// Implicit bool conversion
if (inventory.Remove("wood", 10))
    Console.WriteLine("Removed.");
```

---

## Unique Items

```csharp
// Unique = gets an instance ID, never stacks with other copies
inventory.Add("iron_sword", 1, unique: true);
var sword = inventory.GetItems().First(s => s.Definition.Id == "iron_sword");
Console.WriteLine(sword.InstanceId); // Guid, e.g. 3f2a1b...
```

Unique items are the foundation for affixes, durability, and socketing -- store the instance state in your own data layer, keyed by `InstanceId`.

---

## License

MIT -- use it in any game, commercial or otherwise.

---

> Need a managed server, REST API, and a Godot editor to manage all of this visually?  
> Check out **[InventoryFramework](https://mbaltuncay.gumroad.com/l/qyeyym)** -- the full-stack version.
