using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace BackpackMod.Patches;

/// <summary>
/// Makes police body searches also cover the contents of the backpacks the player carries,
/// by adding one slot UI per backpack slot to the search screen.
/// </summary>
internal static class BodySearchPatch
{
    private static readonly List<ItemSlotUI> _extraSlots = new();
    private static bool _applied;

    /// <summary>Patched lazily (once the Main scene is loaded) instead of at startup, where hooking this UI class crashed the game.</summary>
    public static void Apply(HarmonyLib.Harmony harmony)
    {
        if (_applied)
            return;
        _applied = true;
        try
        {
            foreach (var (name, handler) in new[] { ("Open", nameof(Open)), ("Close", nameof(Close)) })
            {
                var target = AccessTools.GetDeclaredMethods(typeof(BodySearchScreen)).FirstOrDefault(m => m.Name == name);
                if (target == null)
                {
                    Melon<Core>.Logger.Error($"BodySearchScreen.{name} not found");
                    continue;
                }
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(BodySearchPatch), handler));
            }
            Melon<Core>.Logger.Msg("Body search patch applied.");
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Could not patch BodySearchScreen: {ex}");
        }
    }

    public static void Open(BodySearchScreen __instance)
    {
        try
        {
            RemoveExtraSlots(__instance);

            var slots = __instance.slots;
            if (slots == null || slots.Count == 0)
            {
                Melon<Core>.Logger.Msg("Body search: no slot UI built, cannot add backpack slots.");
                return;
            }

            var carried = new HashSet<string>();
            var inventorySlots = PlayerInventory.Instance?.GetAllInventorySlots();
            if (inventorySlots != null)
            {
                foreach (var slot in inventorySlots)
                {
                    var id = slot?.ItemInstance?.Definition?.ID;
                    if (!string.IsNullOrEmpty(id))
                        carried.Add(id);
                }
            }

            var template = slots[0];
            var parent = template.transform.parent;
            int added = 0;
            foreach (var backpack in BackpackTypes.Backpacks)
            {
                if (!carried.Contains(backpack.ID))
                    continue;
                var backpackSlots = backpack.StorageEntity?.ItemSlots;
                if (backpackSlots == null)
                    continue;

                for (int i = 0; i < backpackSlots.Count; i++)
                {
                    var ui = CreateSlotUI(__instance, template, parent, backpackSlots[i]);
                    slots.Add(ui);
                    _extraSlots.Add(ui);
                    added++;
                }
            }

            try
            {
                var rt = parent.GetComponent<RectTransform>();
                var layouts = string.Join(",", parent.GetComponents<Component>().Select(c => c.GetType().Name));
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < slots.Count; i++)
                {
                    var r = slots[i].GetComponent<RectTransform>();
                    sb.Append($"[{i}:{r.anchoredPosition.x:0},{r.anchoredPosition.y:0}|{r.sizeDelta.x:0}x{r.sizeDelta.y:0}] ");
                }
                Melon<Core>.Logger.Msg($"Body search layout: parent '{parent.name}' ({rt.rect.width:0}x{rt.rect.height:0}) components={layouts}; slots {sb}");
            }
            catch (Exception ex)
            {
                Melon<Core>.Logger.Error($"Body search layout diagnostics failed: {ex.Message}");
            }

            var triggerOnTemplate = template.GetComponent<EventTrigger>() != null;
            Melon<Core>.Logger.Msg($"Body search: {added} backpack slot(s) added to {slots.Count - added} existing (template has EventTrigger: {triggerOnTemplate}).");
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in BodySearchPatch.Open: {ex}");
        }
    }

    private static ItemSlotUI CreateSlotUI(BodySearchScreen screen, ItemSlotUI template, Transform parent, ItemSlot slot)
    {
        var go = UnityEngine.Object.Instantiate(template.gameObject, parent);
        go.name = "BackpackSearchSlot";
        go.SetActive(true);
        var ui = go.GetComponent<ItemSlotUI>();
        ui.AssignSlot(slot);

        // Cloned objects don't keep runtime-added listeners: wire the search interactions ourselves
        var oldTrigger = go.GetComponent<EventTrigger>();
        if (oldTrigger != null)
            UnityEngine.Object.Destroy(oldTrigger);
        var trigger = go.AddComponent<EventTrigger>();
        AddEntry(trigger, EventTriggerType.PointerEnter, () => screen.hoveredSlot = ui);
        AddEntry(trigger, EventTriggerType.PointerExit, () =>
        {
            if (screen.hoveredSlot != null && screen.hoveredSlot.Pointer == ui.Pointer)
                screen.hoveredSlot = null;
        });
        AddEntry(trigger, EventTriggerType.PointerDown, () => screen.SlotHeld(ui));
        AddEntry(trigger, EventTriggerType.PointerUp, () => screen.SlotReleased(ui));
        return ui;
    }

    private static void AddEntry(EventTrigger trigger, EventTriggerType type, Action action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((UnityAction<BaseEventData>)(_ => action()));
        trigger.triggers.Add(entry);
    }

    private static void RemoveExtraSlots(BodySearchScreen screen)
    {
        foreach (var ui in _extraSlots)
        {
            if (ui == null)
                continue;
            screen.slots?.Remove(ui);
            UnityEngine.Object.Destroy(ui.gameObject);
        }
        _extraSlots.Clear();
    }

    public static void Close(BodySearchScreen __instance)
    {
        try
        {
            RemoveExtraSlots(__instance);
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in BodySearchPatch.Close: {ex}");
        }
    }
}
