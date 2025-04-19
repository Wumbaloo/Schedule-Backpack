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

            ItemDefinition = new StorableItemDefinition
            {
                ID = name,
                Name = name,
                Description = description,
                BasePurchasePrice = price,
                Category = EItemCategory.Tools,
                Icon = icon,
                StackLimit = 1,
            };

            ItemInstance = new ItemInstance(ItemDefinition, 1)
            {
                ID = name,
            };

            ShopListing = new BackpackListing(this);

            Registry.Instance.AddToRegistry(ItemDefinition);
            CreateStorageEntity();
        }

        private void CreateStorageEntity()
        {
            var templates = UnityEngine.Object.FindObjectsOfType<StorageEntity>();
            if (templates == null || templates.Length == 0)
            {
                MelonLogger.Error("No StorageEntity template found in scene!");
                return;
            }
            StorageEntity = UnityEngine.Object.Instantiate(templates[0]);
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

    public static List<Backpack> Backpacks { get; set; } = new List<Backpack>([
            new Backpack("Small Backpack", "A small backpack for minimal items.", 3, 1, 300, "small.png")
        ]);
}
