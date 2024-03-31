using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace PoisonPuffer.Patches;

[HarmonyPatch]
public class PufferAIPatch {
    [HarmonyPatch(typeof(PufferAI), "shakeTailAnimation", MethodType.Enumerator)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileShakeTailAnimation(IEnumerable<CodeInstruction> instructions) {
        PoisonPuffer.Logger.LogDebug("Searching for Instantiate...");

        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; i++) {
            // Look for the Instantiate call
            if (codes[i].opcode != OpCodes.Call || codes[i].operand is not MethodInfo { Name: "Instantiate" })
                continue;

            PoisonPuffer.Logger.LogDebug("Found!");

            // Inject code to call your method with the instantiated object as a parameter
            var poisonPufferMethod = AccessTools.Method(typeof(PufferAIPatch), nameof(DoSomething),
            [
                typeof(GameObject),
            ]);

            PoisonPuffer.Logger.LogDebug("Injecting method '" + poisonPufferMethod + "'!");

            codes[i + 1] = new(OpCodes.Call, poisonPufferMethod);
            break;
        }

        return codes.AsEnumerable();
    }

    public static void DoSomething(GameObject gameObject) {
        PoisonPuffer.Logger.LogDebug("Found object: " + gameObject);

        gameObject.AddComponent<PoisonTrigger>();
    }
}