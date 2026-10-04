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
    // Size/position of the small backpack icon shown in the corner of every backpack slot
    private static readonly Vector2 BadgeSize = new Vector2(26f, 26f);
    private static readonly Vector2 BadgeOffset = new Vector2(-3f, -3f);

    private static readonly List<ItemSlotUI> _extraSlots = new();
    private static bool _applied;

    // Vanilla position of the search indicator's start/end markers, restored when the screen closes
    private static Vector3 _origStartPos;
    private static Vector3 _origEndPos;
    private static bool _indicatorSaved;

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
            var parentRect = parent.GetComponent<RectTransform>();

            // Reference geometry of the vanilla layout (8 slots): used to keep the indicator's offsets
            Rebuild(parentRect);
            int originalCount = slots.Count;
            float firstX = slots[0].transform.position.x;
            float lastX = slots[originalCount - 1].transform.position.x;
            var start = __instance.SearchIndicatorStart;
            var end = __instance.SearchIndicatorEnd;
            bool canAlign = start != null && end != null && Math.Abs(lastX - firstX) > 1f;
            float startOffset = 0f, endOffset = 0f;
            if (canAlign)
            {
                startOffset = start.position.x - firstX;
                endOffset = end.position.x - lastX;
                _origStartPos = start.position;
                _origEndPos = end.position;
                _indicatorSaved = true;
            }

            // Names of the children the slot prefab really has: anything else on a clone is a leftover
            // runtime-created item UI (the game's own storage-icon ghost) that must not stay behind our items
            var keep = new HashSet<string>();
            var prefab = __instance.ItemSlotPrefab;
            if (prefab != null)
            {
                var prefabTransform = prefab.transform;
                for (int i = 0; i < prefabTransform.childCount; i++)
                    keep.Add(prefabTransform.GetChild(i).name);
            }

            foreach (var backpack in BackpackTypes.Backpacks)
            {
                if (!carried.Contains(backpack.ID))
                    continue;
                var backpackSlots = backpack.StorageEntity?.ItemSlots;
                if (backpackSlots == null)
                    continue;

                for (int i = 0; i < backpackSlots.Count; i++)
                {
                    var ui = CreateSlotUI(__instance, template, parent, backpackSlots[i], backpack.Icon, keep);
                    slots.Add(ui);
                    _extraSlots.Add(ui);
                }
            }

            if (_extraSlots.Count == 0)
                return;

            // The search indicator was laid out for the vanilla slots only: move its start/end markers
            // onto the first/last slot of the extended row, keeping the vanilla offsets
            Rebuild(parentRect);
            if (canAlign)
            {
                float newFirstX = slots[0].transform.position.x;
                float newLastX = slots[slots.Count - 1].transform.position.x;
                var sp = start.position;
                var ep = end.position;
                start.position = new Vector3(newFirstX + startOffset, sp.y, sp.z);
                end.position = new Vector3(newLastX + endOffset, ep.y, ep.z);
                Melon<Core>.Logger.Msg(
                    $"Body search: {_extraSlots.Count} backpack slot(s) added to {originalCount}; " +
                    $"indicator start {sp.x:F0}->{start.position.x:F0}, end {ep.x:F0}->{end.position.x:F0}.");
            }
            else
            {
                Melon<Core>.Logger.Msg(
                    $"Body search: {_extraSlots.Count} backpack slot(s) added to {originalCount}; " +
                    "indicator not aligned (could not read the vanilla layout).");
            }
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error in BodySearchPatch.Open: {ex}");
        }
    }

    private static void Rebuild(RectTransform rect)
    {
        if (rect != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private static ItemSlotUI CreateSlotUI(BodySearchScreen screen, ItemSlotUI template, Transform parent, ItemSlot slot,
        Sprite backpackIcon, HashSet<string> keep)
    {
        var go = UnityEngine.Object.Instantiate(template.gameObject, parent);
        go.name = "BackpackSearchSlot";
        go.SetActive(true);

        // The template's item UI is created at runtime, so the clone carries a stray copy of it
        // (shown as a ghost icon behind the real item): remove it before assigning our slot
        if (keep.Count > 0)
        {
            var t = go.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (keep.Contains(child.name))
                    continue;
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

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

        AddBackpackBadge(go, backpackIcon);
        return ui;
    }

    /// <summary>Small backpack icon in the top-right corner, so backpack slots are told apart from the hotbar.</summary>
    private static void AddBackpackBadge(GameObject slotGo, Sprite icon)
    {
        if (icon == null)
            return;

        var badge = new GameObject("BackpackBadge");
        badge.transform.SetParent(slotGo.transform, false);
        var rt = badge.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = BadgeSize;
        rt.anchoredPosition = BadgeOffset;

        var image = badge.AddComponent<UnityEngine.UI.Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0.95f);
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

        // Put the search indicator's markers back where the game placed them
        if (_indicatorSaved)
        {
            var start = screen.SearchIndicatorStart;
            var end = screen.SearchIndicatorEnd;
            if (start != null)
                start.position = _origStartPos;
            if (end != null)
                end.position = _origEndPos;
            _indicatorSaved = false;
        }
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
