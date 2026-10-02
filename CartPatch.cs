using HarmonyLib;
using Il2CppScheduleOne.UI.Shop;
using MelonLoader;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(Cart))]
internal static class CartPatch
{
    private static readonly List<string> _purchasedBackpackIDs = new();

    [HarmonyPatch("Buy")]
    [HarmonyPrefix]
    public static void BeforeCartBuy(Cart __instance)
    {
        try
        {
            _purchasedBackpackIDs.Clear();
            if (__instance.cartEntries == null || __instance.cartEntries.Count == 0)
                return;

            for (int i = 0; i < __instance.cartEntries.Count; i++)
            {
                var entry = __instance.cartEntries[i];
                if (entry?.Listing == null)
                    continue;

                // Check if this listing's item is a backpack
                var backpack = BackpackTypes.Backpacks.FirstOrDefault(x => x.ItemDefinition == entry.Listing.Item);
                if (backpack != null)
                {
                    _purchasedBackpackIDs.Add(backpack.ID);
                }
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in CartPatch.BeforeCartBuy: {ex}");
        }
    }

    [HarmonyPatch("Buy")]
    [HarmonyPostfix]
    public static void AfterCartBuy(Cart __instance)
    {
        try
        {
            if (_purchasedBackpackIDs.Count == 0)
                return;

            if (Backpack.Instance == null)
            {
                Melon<Core>.Logger.Msg("Backpack.Instance is null during cart purchase");
                return;
            }

            // Equip the first purchased backpack
            var firstBackpackID = _purchasedBackpackIDs[0];
            var backpack = BackpackTypes.Backpacks.FirstOrDefault(x => x.ID == firstBackpackID);
            if (backpack != null)
            {
                Backpack.Instance.EquipBackpack(backpack);
                Melon<Core>.Logger.Msg($"Equipped backpack '{backpack.Name}' after purchase.");
            }

            _purchasedBackpackIDs.Clear();
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in CartPatch.AfterCartBuy: {ex}");
        }
    }
}
