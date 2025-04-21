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
        if (__instance.LocalExtraFiles.Contains("Backpack"))
            return;
        __instance.LocalExtraFiles.Add("Backpack");
    }

    [HarmonyPatch("WriteData")]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void WriteData(Player __instance, string parentFolderPath)
    {
        var backpackStorage = Save.GetBackpackSave();
        __instance.Cast<ISaveable>().WriteSubfile(parentFolderPath, "Backpack", backpackStorage);
    }

    [HarmonyPatch("Load", typeof(PlayerData), typeof(string))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Load(Player __instance, PlayerData data, string containerPath)
    {
        if (!__instance.Loader.TryLoadFile(containerPath, "Backpack", out var contentsString))
            return;

        try
        {
            Save.LoadBackpack(contentsString);
        }
        catch (Exception e)
        {
            Melon<Core>.Logger.Error($"Error while loading backpack data: {e}");
        }
    }

    [HarmonyPatch("Activate")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Activate()
    {
        Backpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("Deactivate")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void Deactivate()
    {
        Backpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("ExitAll")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void ExitAll()
    {
        Backpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("PassOutRecovery")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void PassOutRecovery()
    {
        Backpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("PassOut")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void PassOut()
    {
        Backpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("OnRevived")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void OnRevived()
    {
        Backpack.Instance.SetBackpackEnabled(true);
    }

    [HarmonyPatch("OnDied")]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void OnDied(Player __instance)
    {
        if (!__instance.Owner.IsLocalClient)
            return;

        Backpack.Instance.SetBackpackEnabled(false);
    }

    [HarmonyPatch("LoadInventory")]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void LoadInventory(Player __instance)
    {
        if (!__instance.Owner.IsLocalClient) return;

        foreach (var item in __instance.Inventory)
        {
            if (item == null) continue;
            var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.Name == item.ItemInstance.Name);
            if (backpack != null)
            {
                Backpack.Instance.EquipBackpack(backpack);
                break;
            }
        }
    }
}
