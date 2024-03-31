using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using OpCodes = System.Reflection.Emit.OpCodes;

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

    [HarmonyPatch(typeof(PufferAI), "DoAIInterval")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DoAIIntervalTranspiler(IEnumerable<CodeInstruction> instructions) {
        FindPattern1(ref instructions);
        FindPattern2(ref instructions);

        return instructions;
    }

    private static void FindPattern1(ref IEnumerable<CodeInstruction> instructions) {
        var codeInstructions = instructions.ToList();

        string[] pattern = [
            "ldloc.2 NULL", "ldc.r4 5", "bge.un Label13",
        ];

        var currentIndex = 0;

        for (var index = 0; index < codeInstructions.Count(); index++) {
            var instruction = codeInstructions[index];

            if (!instruction.ToString().Equals(pattern[currentIndex])) {
                currentIndex = 0;
                continue;
            }

            currentIndex += 1;

            if (currentIndex < 2)
                continue;

            codeInstructions[index] = new(OpCodes.Ldc_R4, 10f);
            currentIndex = 0;

            PoisonPuffer.Logger.LogDebug("Found distance!");
        }

        instructions = codeInstructions;
    }

    private static void FindPattern2(ref IEnumerable<CodeInstruction> instructions) {
        var codeInstructions = instructions.ToList();

        string[] pattern = [
            "ldfld float PufferAI::timeSinceAlert", "ldc.r4 1.5", "ble.un Label15",
        ];

        var currentIndex = 0;

        for (var index = 0; index < codeInstructions.Count(); index++) {
            var instruction = codeInstructions[index];

            if (!instruction.ToString().Equals(pattern[currentIndex])) {
                currentIndex = 0;
                continue;
            }

            currentIndex += 1;

            if (currentIndex < 2)
                continue;

            codeInstructions[index] = new(OpCodes.Ldc_R4, .75f);
            currentIndex = 0;

            PoisonPuffer.Logger.LogDebug("Found timeSinceAlert!");
        }

        instructions = codeInstructions;
    }
}