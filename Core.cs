using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BackpackMod.Core), "Backpack", "1.2.0", "Wumbaloo", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod;

public class Core : MelonMod
{
    private const KeyCode DefaultToggleKey = KeyCode.B;

    private MelonPreferences_Entry<string> _toggleKeyEntry;

    /// <summary>Key that opens/closes the backpack, configurable in UserData/MelonPreferences.cfg.</summary>
    public static KeyCode ToggleKey { get; private set; } = DefaultToggleKey;

    public override void OnInitializeMelon()
    {
        var category = MelonPreferences.CreateCategory("Backpack");
        _toggleKeyEntry = category.CreateEntry(
            "ToggleKey",
            DefaultToggleKey.ToString(),
            "Open/close key",
            "Name of a Unity KeyCode (e.g. B, G, F6, LeftAlt, Mouse3). Applied immediately when the file is saved.");
        ReadToggleKey();

        HarmonyInstance.PatchAll();

        Player.onPlayerSpawned += new Action<Player>(OnPlayerSpawned);

        LoggerInstance.Msg($"Successfully loaded! Backpack key: {ToggleKey}");
    }

    public override void OnPreferencesLoaded() => ReadToggleKey();

    public override void OnPreferencesSaved() => ReadToggleKey();

    private void ReadToggleKey()
    {
        if (_toggleKeyEntry == null)
            return;

        var value = _toggleKeyEntry.Value?.Trim();
        if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, true, out KeyCode key) && key != KeyCode.None)
        {
            ToggleKey = key;
            return;
        }

        LoggerInstance.Warning($"Invalid Backpack ToggleKey '{value}' in the preferences, using {DefaultToggleKey}.");
        ToggleKey = DefaultToggleKey;
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        // New game session: fresh shop UI, empty backpacks, and items registered before the save loads
        BackpackMod.Patches.BodySearchPatch.Apply(HarmonyInstance);
        BackpackMod.Patches.ArrestInventoryPatch.Apply(HarmonyInstance);
        ShopManager.Reset();
        try
        {
            BackpackTypes.InitBackpacks();
            BackpackTypes.ClearAllContents();
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error initializing backpacks on scene load: {ex}");
        }
    }

    private void OnPlayerSpawned(Player player)
    {
        if (player.gameObject == null) return;
        if (player.gameObject.TryGetComponent<Backpack>(out _))
        {
            return;
        }
        player.gameObject.AddComponent<Backpack>();
    }
}
