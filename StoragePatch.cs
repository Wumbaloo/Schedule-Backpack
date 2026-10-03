using HarmonyLib;
using Il2CppScheduleOne.Storage;

namespace BackpackMod.Patches;

/// <summary>
/// Backpack storages are plain local StorageEntity components without a NetworkObject,
/// so networked calls would throw. Skip the accessor RPC for them.
/// </summary>
[HarmonyPatch(typeof(StorageEntity))]
internal static class StoragePatch
{
    [HarmonyPatch("SendAccessor")]
    [HarmonyPrefix]
    public static bool SendAccessor(StorageEntity __instance)
    {
        foreach (var backpack in BackpackTypes.Backpacks)
        {
            if (backpack.StorageEntity != null && backpack.StorageEntity.Pointer == __instance.Pointer)
                return false;
        }
        return true;
    }
}
