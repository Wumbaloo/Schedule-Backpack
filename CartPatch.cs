using HarmonyLib;
using Il2CppScheduleOne.UI.Shop;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(Cart))]
internal static class CartPatch
{
    private static readonly List<ShopListing> _purchasedBackpacks = [];

    [HarmonyPatch("Buy")]
    [HarmonyPrefix]
    public static void BeforeCartBuy(Cart __instance)
    {
        _purchasedBackpacks.Clear();
        for (int i = 0; i < __instance.cartEntries.Count; i++)
        {
            var entry = __instance.cartEntries[i];
            if (BackpackTypes.Backpacks.Any(x => x.ShopListing.name == entry.Listing.name)) {
                _purchasedBackpacks.Add(entry.Listing);
            }
        }
    }

    [HarmonyPatch("Buy")]
    [HarmonyPostfix]
    public static void AfterCartBuy(Cart __instance)
    {
        for (int i = 0; i < _purchasedBackpacks.Count; i++) {
            var backpack = BackpackTypes.Backpacks.FirstOrDefault(x => x.ShopListing.name == _purchasedBackpacks[i].name);
            Backpack.Instance.EquipBackpack(backpack);
        }
    }
}
