using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Il2CppScheduleOne.PlayerScripts;

namespace BackpackMod;

[RegisterTypeInIl2Cpp]
public class Backpack(IntPtr ptr) : MonoBehaviour(ptr)
{
    private const KeyCode ToggleKey = KeyCode.B;

    public static Backpack Instance { get; private set; }
    public Save Save { get; } = new Save();

    public BackpackTypes.Backpack CurrentBackpack;

    private ShopManager _shopManager { get; } = new ShopManager();
    private StorageMenu _storageMenu = new StorageMenu();

    private bool _isOpened = false;
    private bool _enabled = true;

    public void Awake()
    {
        Instance = this;
        if (!gameObject)
        {
            Melon<Core>.Logger.Error("GameObject is null.");
            return;
        }
        var storageMenuObject = GameObject.Find("StorageMenu");
        if (storageMenuObject)
        {
            _storageMenu = storageMenuObject.GetComponent<StorageMenu>();
            _storageMenu.onClosed.AddListener((UnityAction)OnStorageMenuClosed);
        }
    }

    private void OnStorageMenuClosed()
    {
        if (Player.Local == null || !Player.Local.IsOwner || BackpackTypes.Backpacks?.Count == 0) return;
        RefreshBackpack();
    }

    public void Open()
    {
        if (CurrentBackpack?.StorageEntity == null || !CurrentBackpack.StorageEntity.CanBeOpened() || !_enabled)
            return;
        _isOpened = true;
        CurrentBackpack.StorageEntity.Open();
    }

    public void Close()
    {
        _isOpened = false;
        CurrentBackpack?.StorageEntity.Close();
    }

    public void EquipBackpack(BackpackTypes.Backpack backpack)
    {
        CurrentBackpack = backpack;
        _enabled = true;
    }

    public void SetBackpackEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled)
        {
            Close();
        } else
        {
            RefreshBackpack();
        }
    }

    public static BackpackTypes.Backpack RefreshBackpack()
    {
        foreach (var item in Player.Local.Inventory)
        {
            if (item == null) continue;
            var backpack = BackpackTypes.Backpacks.FirstOrDefault(b => b.Name == item.ItemInstance?.Name);
            if (backpack != null)
            {
                Instance.EquipBackpack(backpack);
                return backpack;
            }
        }
        Instance.CurrentBackpack = null;
        return null;
    }

    public void Update()
    {
        if (SceneManager.GetActiveScene().name != "Main" || Instance == null)
        {
            Melon<Core>.Logger.Error("Backpack instance is null or not in the main scene.");
            return;
        }

        if (!Input.GetKeyDown(ToggleKey)) return;
        try
        {
            if (_isOpened)
                Close();
            else
                Open();
        }
        catch (Exception ex)
        {
            Melon<Core>.Logger.Error("Error while toggling backpack: " + ex);
        }
    }
}
