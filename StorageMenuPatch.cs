using HarmonyLib;
using Il2CppScheduleOne.UI;
using MelonLoader;

namespace BackpackMod.Patches;

/// <summary>
/// Tracks whether the game's storage menu is currently showing an inventory that is not ours
/// (dealer inventories, containers...). Opening the backpack on top of it replaces that menu and
/// breaks it once we close ours, so the backpack must not open while it is displayed.
/// </summary>
internal static class StorageMenuPatch
{
    private static bool _applied;

    /// <summary>True while the storage menu shows an inventory opened by the game (not by the mod).</summary>
    public static bool OpenedByOthers { get; private set; }

    /// <summary>Set by the mod around its own StorageMenu.Open call.</summary>
    public static bool OpeningOwn { get; set; }

    /// <summary>
    /// Patched lazily (once the Main scene is loaded), like the other UI patches. All overloads of Open
    /// are covered since the game opens dealer/container inventories through different ones.
    /// </summary>
    public static void Apply(HarmonyLib.Harmony harmony)
    {
        OpenedByOthers = false;
        OpeningOwn = false;

        if (_applied)
            return;
        _applied = true;

        try
        {
            foreach (var m in AccessTools.GetDeclaredMethods(typeof(StorageMenu)))
            {
                if (m.Name == "Open")
                    harmony.Patch(m, postfix: new HarmonyMethod(typeof(StorageMenuPatch), nameof(OpenPostfix)));
                else if (m.Name == "Close")
                    harmony.Patch(m, postfix: new HarmonyMethod(typeof(StorageMenuPatch), nameof(ClosePostfix)));
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Could not patch StorageMenu: {ex}");
        }
    }

    private static void OpenPostfix()
    {
        if (!OpeningOwn)
            OpenedByOthers = true;
    }

    private static void ClosePostfix()
    {
        OpenedByOthers = false;
    }
}
