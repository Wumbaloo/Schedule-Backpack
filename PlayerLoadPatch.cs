using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod.Patches;

/// <summary>
/// Loads the backpack contents when the player is loaded from a save.
///
/// Kept out of PlayerPatch on purpose: Harmony aborts a whole patch class on the first patch whose target
/// does not exist, so a game version without Player.Load(PlayerData, string) used to disable every patch
/// declared after it. Here the target is resolved in Prepare(): if it is missing the patch is skipped with a
/// warning, and the fallback below (LoadInventory(string), which receives the same save folder) is used.
/// Positional arguments (__0, __1) are used so a renamed parameter cannot break the patch either.
/// </summary>
internal static class PlayerLoadPatch
{
    internal static MethodBase FindLoad() =>
        AccessTools.Method(typeof(Player), "Load", new[] { typeof(PlayerData), typeof(string) });

    internal static MethodBase FindLoadInventory() =>
        AccessTools.GetDeclaredMethods(typeof(Player)).FirstOrDefault(m =>
            m.Name == "LoadInventory"
            && m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType == typeof(string));

    internal static void LoadBackpack(Player player, string containerPath)
    {
        try
        {
            if (string.IsNullOrEmpty(containerPath))
                return;
            if (!player.Loader.TryLoadFile(containerPath, "Backpack", out var backpackID))
                return;

            Save.LoadBackpack(backpackID);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while loading backpack data: {ex}");
        }
    }
}

[HarmonyPatch]
internal static class PlayerLoadMainPatch
{
    private static bool Prepare()
    {
        if (PlayerLoadPatch.FindLoad() != null)
            return true;
        Melon<Core>.Logger.Warning("Player.Load(PlayerData, string) not found in this game version, using the fallback.");
        return false;
    }

    private static MethodBase TargetMethod() => PlayerLoadPatch.FindLoad();

    [HarmonyPostfix]
    private static void Postfix(Player __instance, string __1) => PlayerLoadPatch.LoadBackpack(__instance, __1);
}

[HarmonyPatch]
internal static class PlayerLoadFallbackPatch
{
    private static MethodBase _target;

    private static bool Prepare()
    {
        if (PlayerLoadPatch.FindLoad() != null)
            return false; // the main patch handles it

        _target = PlayerLoadPatch.FindLoadInventory();
        if (_target == null)
            Melon<Core>.Logger.Error("No way found to load backpack contents from the save in this game version.");
        return _target != null;
    }

    private static MethodBase TargetMethod() => _target;

    [HarmonyPostfix]
    private static void Postfix(Player __instance, string __0) => PlayerLoadPatch.LoadBackpack(__instance, __0);
}
