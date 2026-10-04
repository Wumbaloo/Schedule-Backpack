using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class BackpackEquippable : Equippable
{
    public string backpackID;

    public BackpackEquippable() : base()
    {
        CanInteractWhenEquipped = false;
        CanPickUpWhenEquipped = false;
    }

    public override void Equip(ItemInstance item)
    {
        // The game instantiates a clone of this equippable, so managed fields are lost: resolve via the item.
        var id = item?.Definition?.ID;
        if (string.IsNullOrEmpty(id))
            id = backpackID;
        if (string.IsNullOrEmpty(id))
        {
            Melon<Core>.Logger.Error("BackpackEquippable could not determine the backpack ID!");
            return;
        }

        var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == id);
        if (backpack != null)
        {
            Backpack.Instance.EquipBackpack(backpack);
        }
        else
        {
            Melon<Core>.Logger.Error($"Backpack with ID '{id}' not found!");
        }
    }

    public override void Unequip()
    {
        // Do NOT close the backpack here. Opening a storage menu holsters the held item, which calls
        // Unequip() while the menu is still being built: closing it at that point leaves an empty window.
        // The selection is kept so B still works after switching hotbar slots.
    }
}
