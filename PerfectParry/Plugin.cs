using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Linq;
using System.Reflection;

namespace PerfectParry;

public class PluginInfo
{
    public const string PLUGIN_GUID = "nozwock.PerfectParry";
    public const string PLUGIN_NAME = "Perfect Parry";
    public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    static Harmony harmony;

    void Awake()
    {
        Logger = base.Logger;

        RCGLifeCycle.DontDestroyForever(gameObject);

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_GUID);
        foreach (var method in harmony.GetPatchedMethods())
        {
            Logger.LogInfo($"Patched method: {method.DeclaringType?.FullName}.{method.Name}");
        }
        if (harmony.GetPatchedMethods().Count() == 0)
        {
            Logger.LogError($"Failed to apply Harmony patches.");
        }
    }

    void OnDestroy()
    {
        Logger.LogInfo($"Unloading plugin {PluginInfo.PLUGIN_GUID}");
        harmony?.UnpatchSelf();
    }

    [HarmonyPatch(typeof(PlayerParryState), "Parried")]
    class Patch_PlayerParryState_Parried
    {
        static void Prefix(PlayerParryState __instance)
        {
            __instance.IsAlwaysAccurate = true;
        }
    }
}

