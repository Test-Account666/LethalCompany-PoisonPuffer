using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace PoisonPuffer;

[BepInDependency("TestAccount666.TestAccountCore", "1.1.0")]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PoisonPuffer : BaseUnityPlugin {
    internal static List<AudioClip>? coughAudioClips;
    public static PoisonPuffer Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    internal static ConfigEntry<int> coughVolumeEntry = null!;
    internal static float CoughVolume => coughVolumeEntry.Value / 100F;
    internal static ConfigEntry<int> warningCoolDownEntry = null!;
    internal static long WarningCoolDown => warningCoolDownEntry.Value * 1000;

    private void Awake() {
        Logger = base.Logger;
        Instance = this;

        InitializeConfig();

        Patch();

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (assemblyDirectory is null) {
            Logger.LogError("An error occured while trying to find the assembly directory. Please report this!");
            return;
        }

        coughAudioClips = [
        ];

        var audioPath = Path.Combine(assemblyDirectory, "coughs");

        audioPath = Directory.Exists(audioPath)? audioPath : Path.Combine(assemblyDirectory);

        for (var index = 1; index <= 14; index++)
            StartCoroutine(LoadAudioClipFromFile(new(Path.Combine(audioPath, $"cough{index}.wav")), $"cough{index}"));

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private void InitializeConfig() {
        coughVolumeEntry = Config.Bind("Sound", "Cough Sound Volume", 100,
                                       new ConfigDescription("The volume for coughing sounds",
                                                             new AcceptableValueRange<int>(0, 100)));

        warningCoolDownEntry = Config.Bind("HUD", "Poison Warning Cooldown", 3,
                                           new ConfigDescription(
                                               "The cooldown for warning you about the poison. A cooldown of 0 disables this.",
                                               new AcceptableValueList<int>(0, 5)));
    }

    internal static void Patch() {
        Harmony ??= new(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch() {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

    private static IEnumerator LoadAudioClipFromFile(Uri filePath, string name) {
        using var unityWebRequest = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.WAV);

        yield return unityWebRequest.SendWebRequest();

        if (unityWebRequest.result != UnityWebRequest.Result.Success) {
            Logger.LogError("Failed to load AudioClip: " + unityWebRequest.error);
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(unityWebRequest);
        coughAudioClips?.Add(clip);

        clip.name = name;

        Logger.LogInfo($"Loaded clip '{name}'!");
    }
}