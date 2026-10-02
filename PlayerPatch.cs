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

    [HarmonyPatch("Activate")]
    [HarmonyPrefix]
    public static void Activate()
    {
        try
        {
            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(true);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.Activate: {ex}");
        }
    }

    [HarmonyPatch("Deactivate")]
    [HarmonyPrefix]
    public static void Deactivate()
    {
        try
        {
            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.Deactivate: {ex}");
        }
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    public static void ExitAll()
    {
        try
        {
            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.ExitAll: {ex}");
        }
    }

    [HarmonyPatch("PassOutRecovery")]
    [HarmonyPrefix]
    public static void PassOutRecovery()
    {
        try
        {
            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(true);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.PassOutRecovery: {ex}");
        }
    }

    [HarmonyPatch("PassOut")]
    [HarmonyPrefix]
    public static void PassOut()
    {
        try
        {
            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.PassOut: {ex}");
        }
    }

    [HarmonyPatch("OnRevived")]
    [HarmonyPrefix]
    public static void OnRevived()
    {
        try
        {
            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(true);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.OnRevived: {ex}");
        }
    }

    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    public static void OnDied(Player __instance)
    {
        try
        {
            if (!__instance.Owner.IsLocalClient)
                return;

            if (Backpack.Instance != null)
                Backpack.Instance.SetBackpackEnabled(false);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in PlayerPatch.OnDied: {ex}");
        }
    }

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
