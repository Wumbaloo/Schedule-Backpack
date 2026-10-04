using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using MelonLoader;

namespace BackpackMod.Patches;

/// <summary>
/// Generic arrest handling: while a police body search / arrest is being processed (from the start of
/// the search until a few seconds after the penalties are computed), PlayerInventory.GetAllInventorySlots()
/// also returns the slots of the backpacks the player carries. The game's own rules (illegal item
/// detection, crime count, fine, confiscation) then apply to backpack contents exactly as they do
/// to the inventory, for any item, with no per-item code.
///
/// The window is scoped on purpose: outside of it the inventory (UI, saves, crafting...) is untouched.
/// Patches are applied manually (once) rather than through PatchAll, so they can never be registered twice.
/// </summary>
internal static class ArrestInventoryPatch
{
    private const long OpenMs = 40000;   // max duration after the start of a body search
    private const long LingerMs = 4000;  // keep the window open this long after ProcessCrimeList

    private static bool _applied;
    private static long _windowEnd;

    private static bool Active => Environment.TickCount64 < _windowEnd;

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        if (_applied)
            return;
        _applied = true;

        PatchByName(harmony, typeof(PoliceOfficer), "ConductBodySearch", nameof(OpenWindow), null);
        PatchByName(harmony, typeof(PenaltyHandler), "ProcessCrimeList", null, nameof(ShrinkWindow));
        PatchByName(harmony, typeof(PlayerInventory), "GetAllInventorySlots", null, nameof(ExtendSlots));
    }

    private static void PatchByName(HarmonyLib.Harmony harmony, Type type, string method, string prefix, string postfix)
    {
        try
        {
            foreach (var m in AccessTools.GetDeclaredMethods(type).Where(m => m.Name == method))
            {
                harmony.Patch(m,
                    prefix: prefix != null ? new HarmonyMethod(typeof(ArrestInventoryPatch), prefix) : null,
                    postfix: postfix != null ? new HarmonyMethod(typeof(ArrestInventoryPatch), postfix) : null);
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Could not patch {type.Name}.{method}: {ex.Message}");
        }
    }

    // The game decides crimes while the body search runs: open the window as soon as it starts
    private static void OpenWindow()
    {
        _windowEnd = Environment.TickCount64 + OpenMs;
    }

    // Penalties are computed: keep the window a little longer for the confiscation, then close it
    private static void ShrinkWindow()
    {
        _windowEnd = Math.Min(_windowEnd, Environment.TickCount64 + LingerMs);
    }

    private static void ExtendSlots(ref Il2CppSystem.Collections.Generic.List<ItemSlot> __result)
    {
        if (!Active || __result == null)
            return;

        try
        {
            // Work from the original result only (no recursive call to GetAllInventorySlots)
            var present = new HashSet<IntPtr>();
            var carried = new List<BackpackTypes.Backpack>();
            foreach (var slot in __result)
            {
                if (slot == null)
                    continue;
                present.Add(slot.Pointer);
                var id = slot.ItemInstance?.Definition?.ID;
                if (string.IsNullOrEmpty(id))
                    continue;
                var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == id);
                if (backpack != null && !carried.Contains(backpack))
                    carried.Add(backpack);
            }
            if (carried.Count == 0)
                return;

            // Return a new list: never mutate a list the game may keep internally
            var merged = new Il2CppSystem.Collections.Generic.List<ItemSlot>();
            foreach (var slot in __result)
                merged.Add(slot);

            int added = 0;
            foreach (var backpack in carried)
            {
                var slots = backpack.StorageEntity?.ItemSlots;
                if (slots == null)
                    continue;
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot == null || !present.Add(slot.Pointer))
                        continue;
                    merged.Add(slot);
                    added++;
                }
            }

            if (added > 0)
                __result = merged;
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in ArrestInventoryPatch.ExtendSlots: {ex}");
        }
    }
}
