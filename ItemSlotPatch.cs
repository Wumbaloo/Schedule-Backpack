using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;

namespace BackpackMod.Patches;

/// <summary>
/// Prevents putting a backpack inside a backpack.
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

    [HarmonyPatch("GetCapacityForItem")]
    [HarmonyPostfix]
    public static void GetCapacityForItem(ItemSlot __instance, ItemInstance item, ref int __result)
    {
        if (__result > 0 && IsBackpackItem(item) && IsInsideBackpack(__instance))
            __result = 0;
    }

    [HarmonyPatch("DoesItemMatchHardFilters")]
    [HarmonyPostfix]
    public static void DoesItemMatchHardFilters(ItemSlot __instance, ItemInstance item, ref bool __result)
    {
        if (__result && IsBackpackItem(item) && IsInsideBackpack(__instance))
            __result = false;
    }
}
