using Il2CppScheduleOne.ItemFramework;
using MelonLoader;
using Il2CppScheduleOne.PlayerScripts;

namespace BackpackMod;

public class Save
{
    /// <summary>
    /// Saves the currently equipped backpack ID.
    /// Note: Item contents are stored in ItemSlot data, not here.
    /// </summary>
    public static string GetBackpackSave()
    {
        if (Backpack.Instance?.CurrentBackpack == null)
            return string.Empty;
        return Backpack.Instance.CurrentBackpack.ID;
    }

    /// <summary>
    /// Restores the equipped backpack from save. The actual inventory contents
    /// are restored by the game's inventory system.
    /// </summary>
    public static void LoadBackpack(string backpackID)
    {
        try
        {
            if (string.IsNullOrEmpty(backpackID))
                return;

            if (Player.Local == null || !Player.Local.IsOwner)
                return;

            var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == backpackID);
            if (backpack == null)
            {
                Melon<Core>.Logger.Msg($"Saved backpack ID \"{backpackID}\" not found. It may have been removed.");
                return;
            }

            // Set the current backpack (don't add to inventory, the game handles that)
            Backpack.Instance.EquipBackpack(backpack);
            Melon<Core>.Logger.Msg($"Loaded backpack: {backpack.Name}");
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while loading backpack: {ex}");
        }
    }
}
