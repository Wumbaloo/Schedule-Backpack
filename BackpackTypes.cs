using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI.Shop;
using MelonLoader;
using UnityEngine;

namespace BackpackMod;

public static class BackpackTypes
{
    public class BackpackListing : ShopListing
    {
        public BackpackListing(Backpack backpack)
        {
            name = backpack.Name;
            Item = backpack.ItemDefinition;
            LimitedStock = true;
            DefaultStock = 1;
            CanBeDelivered = false;
            CurrentStock = 1;
        }
    }

    public class Backpack
    {
        public string Name { get; set; } = "Default Backpack";
        public string Description { get; set; } = "This is a default backpack.";

        public int Rows { get; set; } = 2;
        public int Columns { get; set; } = 2;

        public Sprite Icon;

        public int Price { get; set; } = 100;

        public StorageEntity StorageEntity { get; set; } = new StorageEntity();
        public StorableItemDefinition ItemDefinition;
        public BackpackListing ShopListing;
        public ItemInstance ItemInstance;

        public Backpack() { }

        public Backpack(string name, string description, int rows, int columns, int price, string iconPath)
        {
            Name = name;
            Description = description;
            Rows = rows;
            Columns = columns;
            Price = price;

            Sprite icon = SpriteUtils.LoadSpriteFromEmbeddedResource("Backpack.Assets." + iconPath);
            icon.name = name + " (Icon)";
            if (icon == null)
            {
                MelonLogger.Error($"Icon '{iconPath}' not found!");
                return;
            }
            Icon = icon;

            /*BackpackEquippable backpackEquippable = new();
            var go = new GameObject($"BackpackEquippable ({name})");
            var equippable = go.AddComponent<BackpackEquippable>();

            ItemDefinition = new StorableItemDefinition
            {
                ID = name,
                Name = name,
                Description = description,
                BasePurchasePrice = price,
                Category = EItemCategory.Tools,
                Icon = icon,
                StackLimit = 1,
                Equippable = equippable,
            };


            ItemInstance = new ItemInstance(ItemDefinition, 1)
            {
                ID = name,
            };

            backpackEquippable.itemInstance = ItemInstance;

            ShopListing = new BackpackListing(this);

            Registry.Instance.AddToRegistry(ItemDefinition);
            CreateStorageEntity();*/
        }

        public void CreateStorageEntity()
        {
            var templates = UnityEngine.Object.FindObjectsOfType<StorageEntity>();
            if (templates == null || templates.Length == 0)
            {
                MelonLogger.Error("No StorageEntity template found in scene!");
                return;
            }
            var matchTemplate = templates.FirstOrDefault(x => x.name == Name);
            foreach (var template in templates)
            {
               Melon<Core>.Logger.Error($"Loop : {template.name} (we're looking for {Name}).");
            }
            if (matchTemplate == null)
            {
                Melon<Core>.Logger.Error($"No matching template found for {Name}. Using first template.");
                StorageEntity = UnityEngine.Object.Instantiate(templates[0]);
            }
            else
            {
                Melon<Core>.Logger.Error($"Found matching template for {Name}.");
                StorageEntity = matchTemplate;
            }
            StorageEntity.name = Name;
            StorageEntity.StorageEntityName = Name;
            StorageEntity.SlotCount = Rows * Columns;
            StorageEntity.DisplayRowCount = Columns;
            StorageEntity.MaxAccessDistance = float.PositiveInfinity;
            StorageEntity.AccessSettings = StorageEntity.EAccessSettings.Full;
            var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
            for (int i = 0; i < Rows * Columns; i++)
                slots.Add(new ItemSlot());
            StorageEntity.ItemSlots = slots;
        }
    }
    public static void InitBackpacks()
    {
        foreach (var backpack in Backpacks)
        {
            var go = new GameObject($"BackpackEquippable ({backpack.Name})");
            var equippable = go.AddComponent<BackpackEquippable>();

            backpack.ItemDefinition = new StorableItemDefinition
            {
                ID = backpack.Name,
                Name = backpack.Name,
                Description = backpack.Description,
                BasePurchasePrice = backpack.Price,
                Category = EItemCategory.Tools,
                Icon = backpack.Icon,
                StackLimit = 1,
                Equippable = equippable,
            };


            backpack.ItemInstance = new ItemInstance(backpack.ItemDefinition, 1)
            {
                ID = backpack.Name,
            };

            equippable.itemInstance = backpack.ItemInstance;

            backpack.ShopListing = new BackpackListing(backpack);

            Registry.Instance.AddToRegistry(backpack.ItemDefinition);
            backpack.CreateStorageEntity();
        }
    }

    public static List<Backpack> Backpacks { get; set; } = new List<Backpack>([
            new Backpack("Small Backpack", "A small backpack for minimal items.", 3, 1, 300, "small.png"),
            new Backpack("Medium Backpack", "A very standard backpack for various items.", 6, 1, 700, "medium.png"),
            new Backpack("Large Backpack", "A large backpack for big guns and items.", 4, 2, 1500, "big.png"),
        ]);
}
