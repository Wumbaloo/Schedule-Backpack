using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppScheduleOne.PlayerScripts;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class Backpack : MonoBehaviour
{
    public Backpack(IntPtr ptr) : base(ptr) { }

    private const KeyCode ToggleKey = KeyCode.B;

    public static Backpack Instance { get; private set; }

    public BackpackTypes.Backpack CurrentBackpack;
    public ShopManager shopManager;

    private bool _isOpened = false;
    private bool _enabled = true;

    public void Awake()
    {
        Instance = this;
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

    public void EquipBackpack(BackpackTypes.Backpack backpack)
    {
        CurrentBackpack = backpack;
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
            EquipBackpack(owned[0]);
    }

    public void Update()
    {
        // Only process in Main scene
        if (SceneManager.GetActiveScene().name != "Main" || Instance == null)
            return;

        // Check if player can toggle backpack (not in UI, text field, etc.)
        if (!Input.GetKeyDown(ToggleKey))
            return;

        try
        {
            // Skip if player is not ready
            if (Player.Local == null || !Player.Local.IsOwner)
                return;

            if (_isOpened)
            {
                Close();
                return;
            }

            SelectBackpackToOpen();
            Open();
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while toggling backpack: {ex}");
        }
    }
}
