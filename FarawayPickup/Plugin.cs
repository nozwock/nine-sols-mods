using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System.Linq;
using System.Reflection;
using System;
using System.Text;

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

    // static string DebugMonoBehaviour(UnityEngine.MonoBehaviour mb)
    // {
    //     var str = new StringBuilder();
    //     str.AppendLine("Type: " + mb.GetType().FullName);
    //     str.AppendLine("GameObject: " + mb.gameObject.name);
    //     str.AppendLine("Tag: " + mb.gameObject.tag);
    //     str.AppendLine("Layer: " + mb.gameObject.layer);

    //     var components = mb.GetComponents<UnityEngine.Component>();
    //     str.AppendLine("Components:");

    //     foreach (var comp in components)
    //     {
    //         str.AppendLine(" - " + comp.GetType().FullName);
    //     }

    //     return str.ToString();
    // }

    [HarmonyPatch(typeof(DropItem))]
    class Patch_DropItem
    {
        private static readonly AccessTools.FieldRef<DropItem, float> _forceFlyToPlayerCounter =
            AccessTools.FieldRefAccess<DropItem, float>("_forceFlyToPlayerCounter");

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void Update_Prefix(DropItem __instance)
        {
            // DropPickable includes stuff like the Data/Lore Terminals
            if (!__instance.TryGetComponent<DropPickable>(out var _))
                __instance.IsNeedGroundToFlyToPlayer = false;
        }

        [HarmonyPatch("UpdateForceFly")]
        [HarmonyPrefix]
        static void UpdateForceFly_Prefix(DropItem __instance)
        {
            if (!__instance.TryGetComponent<DropPickable>(out var _))
            {
                __instance.ForceFlyToPlayerAfterTime = 1f;
                _forceFlyToPlayerCounter(__instance) = 2f;

                // Unfortunately, couldn't find a way to check for BepInEx debug level.
                // Ideally LogDebug would take a callback and run it only when
                // the level is set to get the log string.
                // Logger.LogDebug($"ForceFly: \n{DebugMonoBehaviour(__instance)}");
            }
        }
    }
}
