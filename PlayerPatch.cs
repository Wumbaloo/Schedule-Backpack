using HarmonyLib;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod.Patches;

[HarmonyPatch(typeof(Player))]
internal static class PlayerPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void Awake(Player __instance)
    {
        try
        {
            if (__instance.LocalExtraFiles.Contains("Backpack"))
                return;
            __instance.LocalExtraFiles.Add("Backpack");
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.Awake: {ex}");
        }
    }

    [HarmonyPatch("WriteData")]
    [HarmonyPostfix]
    public static void WriteData(Player __instance, string parentFolderPath)
    {
        try
        {
            var backpackID = Save.GetBackpackSave();
            __instance.Cast<ISaveable>().WriteSubfile(parentFolderPath, "Backpack", backpackID);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while saving backpack data: {ex}");
        }
    }

    // Player.Load(PlayerData, string) is patched separately (PlayerLoadPatch.cs): a missing overload in another
    // game version must not make Harmony abort this whole class and skip every patch declared after it.

    /// <summary>
    /// Called when the local player spawns/loads.
    /// Enable backpack functionality.
    /// </summary>
    [HarmonyPatch("OnStartClient")]
    [HarmonyPostfix]
    public static void OnStartClient(Player __instance)
    {
        try
        {
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(true);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.OnStartClient: {ex}");
        }
    }

    /// <summary>
    /// Called when the player stops (despawn/unload).
    /// Disable backpack functionality.
    /// </summary>
    [HarmonyPatch("OnStopClient")]
    [HarmonyPostfix]
    public static void OnStopClient(Player __instance)
    {
        try
        {
            // Fires for every player object, including remote players leaving a multiplayer session:
            // only the local player's own despawn must disable the backpack
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.OnStopClient: {ex}");
        }
    }

    /// <summary>
    /// Called when player exits all scenes/areas.
    /// Disable backpack.
    /// </summary>
    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    public static void ExitAll(Player __instance)
    {
        try
        {
            // Also runs for remote players in multiplayer: ignore them
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.ExitAll: {ex}");
        }
    }

    /// <summary>
    /// Called when player becomes unconscious/falls asleep.
    /// Disable backpack access.
    /// </summary>
    [HarmonyPatch("SleepStart")]
    [HarmonyPrefix]
    public static void SleepStart(Player __instance)
    {
        try
        {
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.SleepStart: {ex}");
        }
    }

    /// <summary>
    /// Called when player wakes up from unconscious/sleep.
    /// Re-enable backpack.
    /// </summary>
    [HarmonyPatch("SleepEnd")]
    [HarmonyPrefix]
    public static void SleepEnd(Player __instance)
    {
        try
        {
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(true);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.SleepEnd: {ex}");
        }
    }

    /// <summary>
    /// Called when player dies.
    /// Disable backpack access.
    /// </summary>
    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    public static void OnDied(Player __instance)
    {
        try
        {
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.OnDied: {ex}");
        }
    }

    /// <summary>
    /// Called when player is revived.
    /// Re-enable backpack.
    /// </summary>
    [HarmonyPatch("OnRevived")]
    [HarmonyPrefix]
    public static void OnRevived(Player __instance)
    {
        try
        {
            if (!__instance.IsOwner)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(true);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.OnRevived: {ex}");
        }
    }

    /// <summary>
    /// Called when player inventory is loaded.
    /// Detect and equip backpack from inventory.
    /// </summary>
    [HarmonyPatch("LoadInventory")]
    [HarmonyPostfix]
    public static void LoadInventory(Player __instance)
    {
        try
        {
            if (!__instance.IsOwner)
                return;

            // Search for backpacks in player inventory using the new API
            var inventorySlots = PlayerInventory.Instance?.GetAllInventorySlots();
            if (inventorySlots == null || inventorySlots.Count == 0)
                return;

            foreach (var slot in inventorySlots)
            {
                if (slot?.ItemInstance == null)
                    continue;

                var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ItemInstance?.Definition == slot.ItemInstance.Definition);
                if (backpack != null)
                {
                    Backpack.Instance.EquipBackpack(backpack);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.LoadInventory: {ex}");
        }
    }
}
