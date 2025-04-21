using Il2CppScheduleOne.UI.Shop;
using MelonLoader;

namespace BackpackMod;

public class ShopManager
{
    readonly ShopInterface _shop;

    public ShopManager() {
        var shopUIName = "HardwareStoreInterface (North Store)";
        var shopManager = UnityEngine.GameObject.Find(shopUIName);
        if (shopManager == null)
        {
            Melon<Core>.Logger.Error($"Shop manager '{shopUIName}' not found.");
        }
        _shop = shopManager.GetComponent<ShopInterface>();
        for (int i = 0; i < BackpackTypes.Backpacks.Count; i++)
        {
            var backpack = BackpackTypes.Backpacks[i];
            var listing = backpack.ShopListing;

            Melon<Core>.Logger.Msg($"Backpack \"{backpack.Name}\" added to Dan's Hardware shop.");
            _shop.Listings.Add(listing);
            _shop.CreateListingUI(listing);
        }
    }
}
