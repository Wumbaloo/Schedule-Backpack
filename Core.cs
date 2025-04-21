using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

[assembly: MelonInfo(typeof(BackpackMod.Core), "Backpack", "1.1.0", "Wumbaloo", null)]
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