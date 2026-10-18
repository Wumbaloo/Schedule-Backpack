using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppScheduleOne.PlayerScripts;
using BackpackMod.Patches;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class Backpack : MonoBehaviour
{
    public Backpack(IntPtr ptr) : base(ptr) { }

    public static Backpack Instance { get; private set; }

    public BackpackTypes.Backpack CurrentBackpack;
    public ShopManager shopManager;

    private bool _isOpened = false;
    private bool _enabled = true;

    public void Awake()
    {
        // One component is added per player (remote ones included): only the local player's one must be
        // the Instance, it is claimed in Update() once Player.Local is known.
        if (Instance == null)
            Instance = this;
    }

    private bool IsLocalPlayerComponent()
    {
        var local = Player.Local;
        return local != null && local.gameObject == gameObject;
    }

    private void Start()
    {
        try
        {
            // Idempotent: creates/registers backpacks if needed and (re)builds the shop listings once per session
            BackpackTypes.InitBackpacks();
            shopManager = new ShopManager();
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Failed to initialize backpack mod: {ex}");
        }
    }

    public void Open()
    {
        if (!_enabled || CurrentBackpack?.StorageEntity == null || StorageMenu.Instance == null)
            return;

        // Open through the menu directly: StorageEntity.Open() relies on networking
        _isOpened = true;
        StorageMenu.Instance.Open(CurrentBackpack.StorageEntity, (Il2CppSystem.Action)(() => OnBackpackClosed()));
    }

    public void Close()
    {
        if (_isOpened && CurrentBackpack?.StorageEntity != null)
        {
            StorageMenu.Instance?.Close();
            _isOpened = false;
        }
    }

    private void OnBackpackClosed()
    {
        _isOpened = false;
    }

    // EquipBackpack(...) lives in BackpackExtensions below: Il2CppInterop warns about any method of an injected
    // type that takes a managed-only parameter, even a non-public one.
    internal void ResetState()
    {
        _enabled = true;
        _isOpened = false;
    }

    public void SetBackpackEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled)
        {
            Close();
        }
        else
        {
            RefreshBackpack();
        }
    }

    public static BackpackTypes.Backpack RefreshBackpack()
    {
        if (Player.Local == null || !Player.Local.IsOwner)
            return null;

        // Check equipped item first
        if (PlayerInventory.Instance?.EquippedItem != null)
        {
            var equippedItem = PlayerInventory.Instance.EquippedItem;
            var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ItemInstance?.Definition == equippedItem.Definition);
            if (backpack != null)
            {
                Instance.EquipBackpack(backpack);
                return backpack;
            }
        }

        // If no equipped backpack, search inventory
        var inventorySlots = PlayerInventory.Instance?.GetAllInventorySlots();
        if (inventorySlots != null)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot?.ItemInstance != null)
                {
                    var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ItemInstance?.Definition == slot.ItemInstance.Definition);
                    if (backpack != null)
                    {
                        Instance.EquipBackpack(backpack);
                        return backpack;
                    }
                }
            }
        }

        Instance.CurrentBackpack = null;
        return null;
    }

    /// <summary>
    /// Picks the backpack B should open: the only owned one, otherwise the last
    /// selected one if still in the inventory, otherwise the first owned one.
    /// </summary>
    private void SelectBackpackToOpen()
    {
        var owned = new List<BackpackTypes.Backpack>();
        var inventorySlots = PlayerInventory.Instance?.GetAllInventorySlots();
        if (inventorySlots != null)
        {
            foreach (var slot in inventorySlots)
            {
                var id = slot?.ItemInstance?.Definition?.ID;
                if (string.IsNullOrEmpty(id))
                    continue;
                var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.ID == id);
                if (backpack != null && !owned.Contains(backpack))
                    owned.Add(backpack);
            }
        }

        if (owned.Count == 0)
        {
            CurrentBackpack = null;
            return;
        }

        if (owned.Count == 1 || CurrentBackpack == null || !owned.Contains(CurrentBackpack))
            this.EquipBackpack(owned[0]);
    }

    public void Update()
    {
        // Only process in Main scene
        if (SceneManager.GetActiveScene().name != "Main")
            return;

        // Remote players have their own Backpack component: only the local player's one reacts to the key
        if (!IsLocalPlayerComponent())
            return;
        if (Instance != this)
            Instance = this;
        if (Save.PendingCurrent != null)
        {
            this.EquipBackpack(Save.PendingCurrent);
            Save.PendingCurrent = null;
        }

        if (!Input.GetKeyDown(Core.ToggleKey))
            return;

        try
        {
            // Skip if player is not ready
            if (!Player.Local.IsOwner)
                return;

            // The menu's real state is the source of truth: our flag can be stale if the game closed the menu itself
            var menu = StorageMenu.Instance;
            var menuOpen = menu != null && menu.IsOpen;
            if (_isOpened && (!menuOpen || menu.OpenedStorageEntity != CurrentBackpack?.StorageEntity))
                _isOpened = false;

            if (_isOpened)
            {
                Close();
                return;
            }

            // Another inventory (dealer, container...) is displayed: opening ours on top of it would break it
            if (menuOpen)
            {
                Melon<Core>.Logger.Msg("Backpack key ignored: another storage menu is open.");
                return;
            }

            SelectBackpackToOpen();
            if (!_enabled || CurrentBackpack?.StorageEntity == null)
                Melon<Core>.Logger.Msg($"Backpack key ignored: enabled={_enabled}, backpack={(CurrentBackpack == null ? "none" : CurrentBackpack.Name)}");
            Open();
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while toggling backpack: {ex}");
        }
    }
}

internal static class BackpackExtensions
{
    internal static void EquipBackpack(this Backpack self, BackpackTypes.Backpack backpack)
    {
        self.CurrentBackpack = backpack;
        self.ResetState();
    }
}
