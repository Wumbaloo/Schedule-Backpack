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

    [HarmonyPatch("Load", typeof(PlayerData), typeof(string))]
    [HarmonyPostfix]
    public static void Load(Player __instance, PlayerData data, string containerPath)
    {
        try
        {
            if (!__instance.Loader.TryLoadFile(containerPath, "Backpack", out var backpackID))
                return;

            Save.LoadBackpack(backpackID);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while loading backpack data: {ex}");
        }
    }

    /// <summary>
    /// TODO: These patches need to be updated to match the actual methods in Schedule I 0.4.6f13
    /// Temporary disabled until we identify the correct method names.
    /// The methods Activate, Deactivate, ExitAll, PassOut, PassOutRecovery, OnRevived, OnDied
    /// either don't exist or have different names in the current version.
    /// </summary>

    [HarmonyPatch("LoadInventory")]
    [HarmonyPostfix]
    public static void LoadInventory(Player __instance)
    {
        try
        {
            if (!__instance.Owner.IsLocalClient)
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
