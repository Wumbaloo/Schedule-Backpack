using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI.Handover;

namespace BackpackMod.Patches;

/// <summary>
/// Prevents putting a backpack inside a backpack, or handing it over to an NPC.
/// </summary>
[HarmonyPatch(typeof(ItemSlot))]
internal static class ItemSlotPatch
{
    private static bool IsBackpackItem(ItemInstance item)
    {
        var id = item?.Definition?.ID;
        if (string.IsNullOrEmpty(id))
            return false;
        return BackpackTypes.Backpacks.Any(b => b.ID == id);
    }

    // Backpack slots have no SlotOwner (it would force networked RPCs), so match by slot instance
    private static bool IsInsideBackpack(ItemSlot slot)
    {
        if (slot == null)
            return false;
        foreach (var backpack in BackpackTypes.Backpacks)
        {
            var slots = backpack.StorageEntity?.ItemSlots;
            if (slots == null)
                continue;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].Pointer == slot.Pointer)
                    return true;
            }
        }
        return false;
    }

    // Slots of the hand-over screen (items given to a customer / NPC): the NPC cannot use a backpack and the item
    // would be lost, so it is refused there too
    private static bool IsHandoverSlot(ItemSlot slot)
    {
        var screen = HandoverScreen.Instance;
        if (slot == null || screen == null || !screen.IsOpen)
            return false;
        var slots = screen._customerSlots;
        if (slots == null)
            return false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].Pointer == slot.Pointer)
                return true;
        }
        return false;
    }

    // Slots of an NPC's inventory (trading items with an NPC): same reasoning. The owner is only known as the
    // IItemSlotOwner interface wrapper, so its real class is read from the Il2Cpp object.
    private static bool IsNpcInventorySlot(ItemSlot slot)
    {
        var owner = slot?.SlotOwner;
        if (owner == null)
            return false;
        try
        {
            var klass = Il2CppInterop.Runtime.IL2CPP.il2cpp_object_get_class(owner.Pointer);
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(
                Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(klass)) == "NPCInventory";
        }
        catch
        {
            return false;
        }
    }

    private static bool IsForbiddenSlot(ItemSlot slot) =>
        IsInsideBackpack(slot) || IsHandoverSlot(slot) || IsNpcInventorySlot(slot);

    [HarmonyPatch("GetCapacityForItem")]
    [HarmonyPostfix]
    public static void GetCapacityForItem(ItemSlot __instance, ItemInstance item, ref int __result)
    {
        if (__result > 0 && IsBackpackItem(item) && IsForbiddenSlot(__instance))
            __result = 0;
    }

    [HarmonyPatch("DoesItemMatchHardFilters")]
    [HarmonyPostfix]
    public static void DoesItemMatchHardFilters(ItemSlot __instance, ItemInstance item, ref bool __result)
    {
        if (__result && IsBackpackItem(item) && IsForbiddenSlot(__instance))
            __result = false;
    }
}
