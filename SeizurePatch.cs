using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Product.Packaging;
using MelonLoader;

namespace BackpackMod.Patches;

/// <summary>
/// The game confiscates items from the player's inventory slots only; apply the same
/// confiscation to the backpacks the player carries.
/// </summary>
[HarmonyPatch(typeof(PlayerInventory))]
internal static class SeizurePatch
{
    private static List<BackpackTypes.Backpack> CarriedBackpacks()
    {
        var result = new List<BackpackTypes.Backpack>();
        var slots = PlayerInventory.Instance?.GetAllInventorySlots();
        if (slots == null)
            return result;
        foreach (var slot in slots)
        {
            var id = slot?.ItemInstance?.Definition?.ID;
            if (string.IsNullOrEmpty(id))
                continue;
            var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == id);
            if (backpack != null && !result.Contains(backpack))
                result.Add(backpack);
        }
        return result;
    }

    // Must run before the original: ClearInventory also removes the backpack items themselves
    [HarmonyPatch("RemoveProductFromInventory")]
    [HarmonyPrefix]
    public static void RemoveProduct(EStealthLevel maxStealth)
    {
        try
        {
            int removed = 0;
            foreach (var backpack in CarriedBackpacks())
            {
                var slots = backpack.StorageEntity?.ItemSlots;
                if (slots == null)
                    continue;
                for (int i = 0; i < slots.Count; i++)
                {
                    var product = slots[i]?.ItemInstance?.TryCast<ProductItemInstance>();
                    if (product == null)
                        continue;
                    var packaging = product.AppliedPackaging;
                    var level = packaging != null ? packaging.StealthLevel : EStealthLevel.None;
                    if (level <= maxStealth)
                    {
                        slots[i].ClearStoredInstance(true);
                        removed++;
                    }
                }
            }
            Melon<Core>.Logger.Msg($"Seizure: {removed} product stack(s) removed from backpacks (max stealth {maxStealth}).");
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in SeizurePatch.RemoveProduct: {ex}");
        }
    }

    [HarmonyPatch("ClearInventory")]
    [HarmonyPrefix]
    public static void ClearInventory()
    {
        try
        {
            foreach (var backpack in CarriedBackpacks())
            {
                var slots = backpack.StorageEntity?.ItemSlots;
                if (slots == null)
                    continue;
                for (int i = 0; i < slots.Count; i++)
                    slots[i]?.ClearStoredInstance(true);
            }
            Melon<Core>.Logger.Msg("Seizure: backpack contents cleared with the inventory.");
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in SeizurePatch.ClearInventory: {ex}");
        }
    }
}
