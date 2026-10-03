using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI.Shop;
using MelonLoader;
using UnityEngine;

namespace BackpackMod;

public static class BackpackTypes
{
    public class Backpack
    {
        public string ID { get; set; }
        public string Name { get; set; } = "Default Backpack";
        public string Description { get; set; } = "This is a default backpack.";

        public int Rows { get; set; } = 2;
        public int Columns { get; set; } = 2;

        public Sprite Icon;
        public int Price { get; set; } = 100;

        // Item definition created at runtime
        public StorableItemDefinition ItemDefinition;
        public ItemInstance ItemInstance;

        // Storage for backpack contents
        public StorageEntity StorageEntity { get; set; }

        public Backpack() { }

        public Backpack(string id, string name, string description, int rows, int columns, int price, string iconPath)
        {
            ID = id;
            Name = name;
            Description = description;
            Rows = rows;
            Columns = columns;
            Price = price;

            // Load and cache sprite with proper Unity lifecycle flags
            Sprite icon = SpriteUtils.LoadSpriteFromEmbeddedResource("Backpack.Assets." + iconPath);
            if (icon == null)
            {
                Melon<Core>.Logger.Error($"Icon '{iconPath}' not found!");
                return;
            }
            icon.name = name + " (Icon)";
            icon.texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Icon = icon;
        }

        public void CreateStorageEntity()
        {
            // Create a fresh StorageEntity for this backpack
            var go = new GameObject($"BackpackStorage_{ID}");
            GameObject.DontDestroyOnLoad(go);

            StorageEntity entity = go.AddComponent<StorageEntity>();
            entity.StorageEntityName = Name;
            entity.StorageEntitySubtitle = Description;
            entity.SlotCount = Rows * Columns;
            entity.DisplayRowCount = Columns;
            entity.MaxAccessDistance = float.PositiveInfinity;
            entity.AccessSettings = StorageEntity.EAccessSettings.Full;
            entity.EmptyOnSleep = false;

            // Initialize empty ItemSlots
            var slots = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
            for (int i = 0; i < Rows * Columns; i++)
            {
                var slot = new ItemSlot();
                // No SlotOwner on purpose: an owner would route every change through networked RPCs
                slots.Add(slot);
            }
            entity.ItemSlots = slots;

            StorageEntity = entity;
            Melon<Core>.Logger.Msg($"Created StorageEntity for backpack '{Name}' with {Rows * Columns} slots.");
        }
    }

    public static void InitBackpacks()
    {
        if (Backpacks == null || Backpacks.Count == 0)
        {
            Melon<Core>.Logger.Error("No backpacks to initialize!");
            return;
        }

        foreach (var backpack in Backpacks)
        {
            try
            {
                if (backpack.ItemDefinition == null)
                    CreateDefinition(backpack);

                // (Re-)register: the game drops runtime items when returning to the menu
                if (Registry.Instance != null && !Registry.ItemExists(backpack.ID))
                {
                    Registry.Instance.AddToRegistry(backpack.ItemDefinition);
                    Melon<Core>.Logger.Msg($"Registered backpack '{backpack.Name}' (ID: {backpack.ID})");
                }
            }
            catch (Exception ex)
            {
                Melon<Core>.Logger.Error($"Failed to initialize backpack '{backpack.Name}': {ex}");
            }
        }
    }

    private static void CreateDefinition(Backpack backpack)
    {
        // Create GameObject for the Equippable component
        var go = new GameObject($"BackpackEquippable_{backpack.ID}");
        GameObject.DontDestroyOnLoad(go);
        var equippable = go.AddComponent<BackpackEquippable>();

        // Create ItemDefinition from the backpack template
        var def = ScriptableObject.CreateInstance<StorableItemDefinition>();
        def.name = backpack.Name;
        // ID/Name/Icon are required by the registry, shop and inventory UI
        def.ID = backpack.ID;
        def.Name = backpack.Name;
        def.Description = backpack.Description;
        def.Icon = backpack.Icon;
        def.Category = Il2CppScheduleOne.Core.Items.Framework.EItemCategory.Storage;
        def.legalStatus = Il2CppScheduleOne.Core.Items.Framework.ELegalStatus.Legal;
        def.StackLimit = 1;
        def.ShopCategories = new Il2CppSystem.Collections.Generic.List<ShopListing.CategoryInstance>();
        def.BasePurchasePrice = backpack.Price;
        def.Equippable = equippable;
        def.hideFlags = HideFlags.DontUnloadUnusedAsset;
        backpack.ItemDefinition = def;

        // Create default ItemInstance
        backpack.ItemInstance = def.GetDefaultInstance(1);
        equippable.itemInstance = backpack.ItemInstance;
        equippable.backpackID = backpack.ID;

        backpack.CreateStorageEntity();
        Melon<Core>.Logger.Msg($"Created backpack '{backpack.Name}' (ID: {backpack.ID})");
    }

    /// <summary>Empties every backpack (used before loading a save).</summary>
    public static void ClearAllContents()
    {
        foreach (var backpack in Backpacks)
        {
            var slots = backpack.StorageEntity?.ItemSlots;
            if (slots == null)
                continue;
            for (int i = 0; i < slots.Count; i++)
                slots[i]?.ClearStoredInstance(true);
        }
    }

    public static List<Backpack> Backpacks { get; set; } = new List<Backpack>
    {
        new Backpack("bp_small", "Small Backpack", "A small backpack for minimal items.", 3, 1, 300, "small.png"),
        new Backpack("bp_medium", "Medium Backpack", "A very standard backpack for various items.", 6, 1, 700, "medium.png"),
        new Backpack("bp_large", "Large Backpack", "A large backpack for big guns and items.", 4, 2, 1500, "big.png"),
    };
}
