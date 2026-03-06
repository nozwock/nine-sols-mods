using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Linq;
using System.Reflection;

namespace PerfectParry;

[BepInAutoPlugin(id: "nozwock.PerfectParry")]
public partial class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    static Harmony harmony;

    void Awake()
    {
        Logger = base.Logger;

        RCGLifeCycle.DontDestroyForever(gameObject);

        Logger.LogInfo($"Plugin {Id} is loaded!");

        harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Id);
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
        Logger.LogInfo($"Unloading plugin {Id}");
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

