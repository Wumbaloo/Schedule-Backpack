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
    private static bool _initialized = false;

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
        // Only initialize backpacks once, even if multiple players spawn
        if (!_initialized)
        {
            try
            {
                BackpackTypes.InitBackpacks();
                shopManager = new ShopManager();
                _initialized = true;
                Melon<Core>.Logger.Msg("Backpack mod initialized successfully.");
            }
            catch (Exception ex)
            {
                Melon<Core>.Logger.Error($"Failed to initialize backpack mod: {ex}");
            }
        }
    }

    public void Open()
    {
        if (!_enabled || CurrentBackpack?.StorageEntity == null)
            return;

        if (!CurrentBackpack.StorageEntity.CanBeOpened())
        {
            Melon<Core>.Logger.Msg($"Backpack '{CurrentBackpack.Name}' cannot be opened.");
            return;
        }

        _isOpened = true;
        CurrentBackpack.StorageEntity.Open();

        // Subscribe to close event using Action delegate
        if (CurrentBackpack.StorageEntity.onClosed != null)
            CurrentBackpack.StorageEntity.onClosed += (Il2CppSystem.Action)(() => OnBackpackClosed());
        else
            CurrentBackpack.StorageEntity.onClosed = (Il2CppSystem.Action)(() => OnBackpackClosed());
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
        RefreshBackpack();
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
                Close();
            else
                Open();
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error($"Error while toggling backpack: {ex}");
        }
    }
}
