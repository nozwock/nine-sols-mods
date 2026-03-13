using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TeleportFromAnyNode;

[BepInAutoPlugin(id: "nozwock.TeleportFromAnyNode")]
[BepInIncompatibility("TeleportFromAnywhere")]
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
            Logger.LogInfo($"Patched method: {method.DeclaringType.FullName}.{method.Name}");
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

    static MethodBase GetAsyncMethod(System.Type type, string name)
    {
        var targetMethod = AccessTools.Method(type, name);
        var stateMachineAttr = targetMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        var moveNextMethod = AccessTools.Method(stateMachineAttr.StateMachineType, "MoveNext");
        return moveNextMethod;
    }

    [HarmonyPatch]
    class Patches
    {
        // TeleportPointMatchSavePanelCondition
        // It checks if SavePanel's CurrentSavePoint is equal to given Teleport Point (Pavilion)
        //
        // Condition is at least attached to:
        // - "Teleport" Button in SavePanel (Root Node UI)
        // - "Teleport" UITabsItem in TabsUI
        [HarmonyPatch(
            typeof(TeleportPointMatchSavePanelCondition),
            nameof(TeleportPointMatchSavePanelCondition.isValid),
            MethodType.Getter)]
        class Patch_TeleportPointMatchSavePanelCondition_get_isValid
        {
            const string savePanelPavilionButtonName = "GoToAG 回議會";
            const string savePanelTeleportButtonName = "Teleport 神遊";
            const string tabsUiTeleportTabName = "TeleportPanel Tab";

            static bool shouldShowTeleport = false;

            static void Postfix(TeleportPointMatchSavePanelCondition __instance, ref bool __result)
            {
                if (__result) return;

                // Check for "Pavilion" button visibility only when SavePanel is opened
                var parentName = __instance.transform.parent?.name;
                if (parentName == savePanelTeleportButtonName)
                {
                    var ui = SingletonBehaviour<UIManager>.Instance;
                    var jumpPavilionButton = ui
                        .SavePointUI
                        .allButtons
                        .First(btn => btn.name == savePanelPavilionButtonName);
                    shouldShowTeleport = jumpPavilionButton?.gameObject.activeInHierarchy == true;
                }

                if (shouldShowTeleport
                    && (parentName == savePanelTeleportButtonName
                    || parentName == tabsUiTeleportTabName))
                {
                    __result = true;
                }
            }
        }

        // Root Node buttons in the Teleport Tab panel
        // Emulate how jump to Pavilion button works (sets current save point as LastSaveItem) for Teleport button
        [HarmonyPatch]
        class Patch_TeleportPointButton_SubmitImplementation
        {
            static MethodBase TargetMethod()
            {
                return GetAsyncMethod(typeof(TeleportPointButton), nameof(TeleportPointButton.SubmitImplementation));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var patched = false;
                foreach (var code in instructions)
                {
                    if (!patched
                        && code.Calls(AccessTools.Method(typeof(SavePanel), nameof(SavePanel.ClearCurrentSavePoint))))
                    {
                        patched = true;
                        yield return Transpilers.EmitDelegate(() =>
                        {
                            var core = SingletonBehaviour<GameCore>.Instance;
                            var currentSavePoint = core.savePanelUiController.CurrentSavePoint;
                            // AGSavePoint is pavilion teleport point (AG_S2_YiBase)
                            if (currentSavePoint != null
                                && currentSavePoint.name != core.allTeleportPoints.AGSavePoint.name)
                            {
                                core.allTeleportPoints.SetLastSaveItem(currentSavePoint);
                            }
                        });
                    }

                    yield return code;
                }
            }
        }
    }
}
