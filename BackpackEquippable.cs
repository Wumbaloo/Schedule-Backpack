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
        if (string.IsNullOrEmpty(backpackID))
        {
            Melon<Core>.Logger.Error("BackpackEquippable has no backpackID set!");
            return;
        }

        var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == backpackID);
        if (backpack != null)
        {
            Backpack.Instance.EquipBackpack(backpack);
        }
        else
        {
            Melon<Core>.Logger.Error($"Backpack with ID '{backpackID}' not found!");
        }
    }

    public override void Unequip()
    {
        Backpack.Instance.SetBackpackEnabled(false);
    }
}
