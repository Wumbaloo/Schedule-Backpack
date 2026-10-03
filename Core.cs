using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

[assembly: MelonInfo(typeof(BackpackMod.Core), "Backpack", "1.2.0", "Wumbaloo", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BackpackMod;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.PatchAll();
        
        Player.onPlayerSpawned += new Action<Player>(OnPlayerSpawned);

        LoggerInstance.Msg("Successfully loaded!");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        // New game session: fresh shop UI, empty backpacks, and items registered before the save loads
        BackpackMod.Patches.BodySearchPatch.Apply(HarmonyInstance);
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