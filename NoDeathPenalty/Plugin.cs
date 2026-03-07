using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace NoDeathPenalty;

// Slightly modified version of https://github.com/Baiker000/NoDeathPenaltyNineSols
[BepInAutoPlugin(id: "nozwock.NoDeathPenalty")]
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

    // https://stackoverflow.com/questions/77435863/using-harmony-to-patch-the-real-content-of-an-async-method-for-a-unity-game
    static MethodBase GetAsyncMethod(System.Type type, string name)
    {
        var targetMethod = AccessTools.Method(type, name);
        var stateMachineAttr = targetMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        var moveNextMethod = AccessTools.Method(stateMachineAttr.StateMachineType, "MoveNext");
        return moveNextMethod;
    }

    [HarmonyPatch]
    class Patch_PlayerGamePlayData_PlayerDeathPenalty
    {
        static MethodBase TargetMethod()
        {
            return GetAsyncMethod(typeof(PlayerGamePlayData), nameof(PlayerGamePlayData.PlayerDeathPenalty));
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_1
                    && codes[i + 1].opcode == OpCodes.Ldc_I4_0
                    && codes[i + 2].opcode == OpCodes.Call
                    && codes[i + 2].operand.ToString().Contains("set_CurrentGold"))
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i + 1].opcode = OpCodes.Nop;
                    codes[i + 2].opcode = OpCodes.Nop;
                }
                if (codes[i].opcode == OpCodes.Ldloc_1
                    && codes[i + 1].opcode == OpCodes.Ldc_I4_0
                    && codes[i + 2].opcode == OpCodes.Call
                    && codes[i + 2].operand.ToString().Contains("set_CurrentExp"))
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i + 1].opcode = OpCodes.Nop;
                    codes[i + 2].opcode = OpCodes.Nop;
                }
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PlayerDeadRecord), nameof(PlayerDeadRecord.StorePlayerDataBeforeDead))]
    class Patch_PlayerDeadRecord_StorePlayerDataBeforeDead
    {
        static void Postfix(PlayerDeadRecord __instance)
        {
            __instance.ContainEXP.CurrentValue = 0;
            __instance.ContainMoney.CurrentValue = 0;
        }
    }
}

