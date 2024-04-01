using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace PoisonPuffer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class PoisonPuffer : BaseUnityPlugin {
    internal static List<AudioClip>? coughAudioClips;
    public static PoisonPuffer Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    private void Awake() {
        Logger = base.Logger;
        Instance = this;

        Patch();

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (assemblyDirectory is null) {
            Logger.LogError("An error occured while trying to find the assembly directory. Please report this!");
            return;
        }

        coughAudioClips = [
        ];

        for (var index = 1; index <= 14; index++)
            StartCoroutine(LoadAudioClipFromFile(new(Path.Combine(assemblyDirectory, "coughs", $"cough{index}.wav")),
                                                 $"cough{index}"));

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
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