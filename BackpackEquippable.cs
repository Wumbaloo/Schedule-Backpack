using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class BackpackEquippable : Equippable
{
    public BackpackEquippable() : base()
    {
        CanInteractWhenEquipped = false;
        CanPickUpWhenEquipped= false;
    }

    public override void Equip(ItemInstance item)
    {
        var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.Name == item.Name);
        if (backpack != null)
        {
            Backpack.Instance.EquipBackpack(backpack);
        }
    }

    public override void Unequip()
    {
    }
}
