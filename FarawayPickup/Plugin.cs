using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.Linq;
using System.Reflection;
using System;

namespace FarawayPickup;

public class PluginInfo
{
    public const string PLUGIN_GUID = "nozwock.FarawayPickup";
    public const string PLUGIN_NAME = "Faraway Pickup";
    public const string PLUGIN_VERSION = "1.0.0";
}


[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_GUID);
        foreach (var method in harmony.GetPatchedMethods())
        {
            Logger.LogInfo($"Patched method: {method.DeclaringType?.FullName}.{method.Name}");
        }
        if (harmony.GetPatchedMethods().Count() == 0)
        {
            Logger.LogError("Failed to apply Harmony patches.");
        }
    }

    [HarmonyPatch(typeof(DropItem))]
    [HarmonyPatch("Update")]
    class Patch_DropItem_Update
    {
        static void Prefix(DropItem __instance)
        {
            __instance.IsNeedGroundToFlyToPlayer = false;
        }
    }

    [HarmonyPatch(typeof(DropItem))]
    [HarmonyPatch("UpdateForceFly")]
    class Patch_DropItem_UpdateForceFly
    {
        private static readonly AccessTools.FieldRef<DropItem, float> _forceFlyToPlayerCounter =
            AccessTools.FieldRefAccess<DropItem, float>("_forceFlyToPlayerCounter");

        static void Prefix(DropItem __instance)
        {
            __instance.ForceFlyToPlayerAfterTime = 1f;
            _forceFlyToPlayerCounter(__instance) = 2f;
        }
    }
}
