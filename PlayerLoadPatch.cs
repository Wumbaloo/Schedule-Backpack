using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod.Patches;

/// <summary>
/// Loads the backpack contents when the player is loaded from a save.
///
/// Kept out of PlayerPatch on purpose: Harmony aborts a whole patch class on the first patch whose target
/// does not exist, so a game version without Player.Load(PlayerData, string) used to disable every patch
/// declared after it. Here the target is resolved in Prepare(): if it is missing the patch is skipped with a
/// warning and the fallback below is used.
///
/// Game versions:
///  - up to 0.4.6: Player.Load(PlayerData, string containerPath) receives the player's save folder.
///  - 0.4.7+: that method is gone and Player.LoadInventory(string) now receives the inventory JSON (not a path),
///    so the fallback hooks PlayerLoader.Load(string mainPath) and reads Backpack.json from the save folder itself.
/// Positional arguments (__0, __1) are used so a renamed parameter cannot break the patch either.
/// </summary>
internal static class PlayerLoadPatch
{
    private const string BackpackFileName = "Backpack.json";

    internal static MethodBase FindLoad() =>
        AccessTools.Method(typeof(Player), "Load", new[] { typeof(PlayerData), typeof(string) });

    internal static MethodBase FindPlayerLoaderLoad() =>
        AccessTools.DeclaredMethod(typeof(PlayerLoader), "Load", new[] { typeof(string) });

    /// <summary>Old game versions: containerPath is the player's save folder.</summary>
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

    /// <summary>
    /// New game versions: mainPath is the players folder (or a player's folder). Reads the Backpack file directly.
    /// </summary>
    internal static void LoadBackpackFromPath(string mainPath)
    {
        try
        {
            if (string.IsNullOrEmpty(mainPath))
                return;

            var file = FindBackpackFile(mainPath);
            if (file == null)
            {
                Melon<Core>.Logger.Msg($"No {BackpackFileName} found under '{mainPath}' (new save or no backpack yet).");
                return;
            }

            Melon<Core>.Logger.Msg($"Loading backpack data from '{file}'.");
            Save.LoadBackpack(File.ReadAllText(file));
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while loading backpack data: {ex}");
        }
    }

    private static string FindBackpackFile(string mainPath)
    {
        if (File.Exists(mainPath))
            mainPath = Path.GetDirectoryName(mainPath);
        if (!Directory.Exists(mainPath))
            return null;

        var direct = Path.Combine(mainPath, BackpackFileName);
        if (File.Exists(direct))
            return direct;

        var candidates = Directory.GetDirectories(mainPath)
            .Select(d => Path.Combine(d, BackpackFileName))
            .Where(File.Exists)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count <= 1)
            return candidates.FirstOrDefault();

        // Several players saved in this game (multiplayer host): prefer the local player's folder
        var localCode = Player.Local?.PlayerCode;
        if (!string.IsNullOrEmpty(localCode))
        {
            var match = candidates.FirstOrDefault(f =>
            {
                var playerJson = Path.Combine(Path.GetDirectoryName(f), "Player.json");
                return File.Exists(playerJson) && File.ReadAllText(playerJson).Contains(localCode);
            });
            if (match != null)
                return match;
        }

        Melon<Core>.Logger.Warning($"{candidates.Count} players have a backpack save, using the first one.");
        return candidates[0];
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

        _target = PlayerLoadPatch.FindPlayerLoaderLoad();
        if (_target == null)
            Melon<Core>.Logger.Error("No way found to load backpack contents from the save in this game version.");
        return _target != null;
    }

    private static MethodBase TargetMethod() => _target;

    [HarmonyPostfix]
    private static void Postfix(string __0) => PlayerLoadPatch.LoadBackpackFromPath(__0);
}
