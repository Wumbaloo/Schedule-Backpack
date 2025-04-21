using Il2CppScheduleOne.ItemFramework;
using MelonLoader;
using Il2CppScheduleOne.PlayerScripts;

namespace BackpackMod;

public class Save
{
    public static string GetBackpackSave()
    {
        if (Backpack.Instance.CurrentBackpack == null) return string.Empty;
        return Backpack.Instance.CurrentBackpack.Name;
    }

    public static void LoadBackpack(string contentsString)
    {
        try
        {
            if (string.IsNullOrEmpty(contentsString) || contentsString == "{}") return;

            if (!Player.Local.IsOwner)
                return;

            var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.Name == contentsString);
            if (backpack == null)
            {
                Melon<Core>.Logger.Error($"Backpack \"{contentsString}\" not found.");
                return;
            }
            Backpack.Instance.CurrentBackpack = backpack;
            var slot = new ItemSlot
            {
                ItemInstance = Backpack.Instance.CurrentBackpack.ItemInstance,
            };

            var firstEmptyIndex = Player.Local.Inventory.Select((x, i) => new { x, i })
                .FirstOrDefault(x => x.x == null || x.x.ItemInstance == null)?.i ?? -1;
            if (firstEmptyIndex != -1)
            {
                Player.Local.Inventory[firstEmptyIndex] = slot;
            } else
            {
                Melon<Core>.Logger.Error("No empty slot found in inventory.");
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error("Error while loading backpack: " + ex);
        }
    }
}
