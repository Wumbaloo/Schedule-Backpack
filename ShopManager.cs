using Il2CppScheduleOne.UI.Shop;
using MelonLoader;
using UnityEngine;
using System.Linq;

namespace BackpackMod;

public class ShopManager
{
    private static bool _shopInitialized = false;

    private static readonly Dictionary<string, ShopListing> _listings = new();

    public static void Reset()
    {
        _shopInitialized = false;
        _listings.Clear();
    }

    /// <summary>Backpacks are limited to one purchase: sold out once bought.</summary>
    public static void ApplyStock()
    {
        foreach (var backpack in BackpackTypes.Backpacks)
        {
            if (!_listings.TryGetValue(backpack.ID, out var listing) || listing == null)
                continue;
            int stock = backpack.Purchased ? 0 : 1;
            listing.DefaultStock = stock;
            listing.CurrentStock = stock;
        }
    }

    public ShopManager()
    {
        // Only initialize shop once to avoid duplicate listings
        if (_shopInitialized)
            return;

        try
        {
            var shopUIName = "HardwareStoreInterface (North Store)";
            var shopUIObject = GameObject.Find(shopUIName);

            if (shopUIObject == null)
            {
                Melon<Core>.Logger.Msg($"Shop UI '{shopUIName}' not found. Backpacks will not appear in shop.");
                return;
            }

            var shopInterface = shopUIObject.GetComponent<ShopInterface>();
            if (shopInterface == null)
            {
                Melon<Core>.Logger.Error("ShopInterface component not found on shop UI.");
                return;
            }

            // Add backpack listings to shop
            for (int i = 0; i < BackpackTypes.Backpacks.Count; i++)
            {
                var backpack = BackpackTypes.Backpacks[i];
                if (backpack?.ItemDefinition == null)
                {
                    Melon<Core>.Logger.Msg($"Backpack '{backpack?.Name}' has null ItemDefinition, skipping.");
                    continue;
                }

                // Create shop listing
                var listing = new BackpackListing(backpack);

                // Check if this listing is already in the shop (to avoid duplicates)
                bool alreadyExists = false;
                foreach (var existingListing in shopInterface.Listings)
                {
                    if (existingListing?.Item == backpack.ItemDefinition)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                {
                    Melon<Core>.Logger.Msg($"Backpack '{backpack.Name}' already exists in shop, skipping.");
                    continue;
                }

                if (backpack.Purchased)
                {
                    listing.DefaultStock = 0;
                    listing.CurrentStock = 0;
                }
                _listings[backpack.ID] = listing;
                shopInterface.Listings.Add(listing);
                shopInterface.CreateListingUI(listing);
                Melon<Core>.Logger.Msg($"Added backpack '{backpack.Name}' to shop.");
            }

            _shopInitialized = true;
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error initializing shop manager: {ex}");
        }
    }

    /// <summary>
    /// Wrapper class to create shop listings for backpack items.
    /// </summary>
    private class BackpackListing : ShopListing
    {
        public BackpackListing(BackpackTypes.Backpack backpack)
        {
            name = backpack.Name;
            Item = backpack.ItemDefinition;
            LimitedStock = true;
            DefaultStock = 1;
            CanBeDelivered = false;
            CurrentStock = 1;
        }
    }
}
