using System.Text.Json;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;

namespace BackpackMod;

public class Save
{
    private class SaveModel
    {
        public string Current { get; set; } = string.Empty;
        public Dictionary<string, string> Contents { get; set; } = new();
    }

    /// <summary>
    /// Serializes the equipped backpack ID and the contents of every backpack.
    /// The backpack items themselves are saved by the game (inventory / storages).
    /// </summary>
    public static string GetBackpackSave()
    {
        var model = new SaveModel { Current = Backpack.Instance?.CurrentBackpack?.ID ?? string.Empty };
        foreach (var backpack in BackpackTypes.Backpacks)
        {
            var slots = backpack.StorageEntity?.ItemSlots;
            if (slots == null)
                continue;
            try
            {
                model.Contents[backpack.ID] = new ItemSet(slots).GetJSON();
            }
            catch (Exception ex)
            {
                Melon<Core>.Logger.Error($"Error while saving contents of '{backpack.Name}': {ex}");
            }
        }
        return JsonSerializer.Serialize(model);
    }

    /// <summary>
    /// Restores backpack contents and the equipped backpack from save.
    /// </summary>
    public static void LoadBackpack(string saved)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(saved))
                return;

            BackpackTypes.InitBackpacks();

            SaveModel model = null;
            try
            {
                model = JsonSerializer.Deserialize<SaveModel>(saved);
            }
            catch (JsonException)
            {
                // Legacy save: plain backpack ID / name
            }

            if (model == null)
            {
                var sep = saved.LastIndexOf("|||", StringComparison.Ordinal);
                var legacy = (sep >= 0 ? saved.Substring(sep + 3) : saved).Trim();
                model = new SaveModel { Current = legacy };
            }

            BackpackTypes.ClearAllContents();
            foreach (var pair in model.Contents)
            {
                var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == pair.Key);
                var slots = backpack?.StorageEntity?.ItemSlots;
                if (slots == null)
                    continue;
                if (ItemSet.TryDeserialize(pair.Value, out var deserialized))
                    deserialized.LoadTo(slots);
                else
                    Melon<Core>.Logger.Error($"Could not deserialize contents of '{backpack.Name}'.");
            }

            var current = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == model.Current || b.Name == model.Current);
            if (current != null && Backpack.Instance != null)
            {
                Backpack.Instance.EquipBackpack(current);
                Melon<Core>.Logger.Msg($"Loaded backpack: {current.Name}");
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while loading backpack: {ex}");
        }
    }
}
