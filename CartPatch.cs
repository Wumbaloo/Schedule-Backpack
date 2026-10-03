using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Shop;
using MelonLoader;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(Cart))]
internal static class CartPatch
{
    private static readonly List<string> _purchasedBackpackIDs = new();

    /// <summary>Backpacks are limited to one per save: refuse adding a sold-out one (or a second copy) to the cart.</summary>
    private static bool AllowQuantity(Cart cart, ShopListing listing, int wantedTotal)
    {
        var backpack = BackpackTypes.Backpacks.FirstOrDefault(x => listing != null && x.ItemDefinition == listing.Item);
        if (backpack == null)
            return true;
        return !backpack.Purchased && wantedTotal <= 1;
    }

    [HarmonyPatch("AddItem")]
    [HarmonyPrefix]
    public static bool BeforeAddItem(Cart __instance, ShopListing listing, int quantity)
    {
        try { return AllowQuantity(__instance, listing, __instance.GetCartCount(listing) + quantity); }
        catch (Exception ex) { Melon<Core>.Logger.Error($"Error in CartPatch.BeforeAddItem: {ex}"); return true; }
    }

    [HarmonyPatch("SetItemQuantity")]
    [HarmonyPrefix]
    public static bool BeforeSetItemQuantity(Cart __instance, ShopListing listing, int quantity)
    {
        try { return AllowQuantity(__instance, listing, quantity); }
        catch (Exception ex) { Melon<Core>.Logger.Error($"Error in CartPatch.BeforeSetItemQuantity: {ex}"); return true; }
    }

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

            // Only count backpacks that actually reached the inventory (purchase may have failed)
            var inventoryIDs = new HashSet<string>();
            var inventorySlots = PlayerInventory.Instance?.GetAllInventorySlots();
            if (inventorySlots != null)
            {
                foreach (var slot in inventorySlots)
                {
                    var itemID = slot?.ItemInstance?.Definition?.ID;
                    if (!string.IsNullOrEmpty(itemID))
                        inventoryIDs.Add(itemID);
                }
            }
            _purchasedBackpackIDs.RemoveAll(id => !inventoryIDs.Contains(id));
            if (_purchasedBackpackIDs.Count == 0)
                return;

            foreach (var id in _purchasedBackpackIDs)
            {
                var bought = BackpackTypes.Backpacks.FirstOrDefault(x => x.ID == id);
                if (bought != null)
                    bought.Purchased = true;
            }
            ShopManager.ApplyStock();

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

[HarmonyPatch(typeof(ListingUI))]
internal static class ListingUIPatch
{
    // Sold-out backpacks (already bought) must not be addable to the cart
    [HarmonyPatch("CanAddToCart")]
    [HarmonyPostfix]
    public static void CanAddToCart(ListingUI __instance, ref bool __result)
    {
        if (!__result)
            return;
        var item = __instance.Listing?.Item;
        var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => item != null && b.ItemDefinition == item);
        if (backpack != null && backpack.Purchased)
            __result = false;
    }
}
