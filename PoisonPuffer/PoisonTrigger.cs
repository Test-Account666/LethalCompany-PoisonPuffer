using System.Collections.Generic;
using TestAccountCore;
using UnityEngine;
using Random = System.Random;

namespace PoisonPuffer;

public class PoisonTrigger : MonoBehaviour {
    private readonly Random _random = new();
    private int? _instanceId;
    private static readonly List<int> _PreviousCoughs = new(3);
    private long _nextCheck;
    private bool _destroy;

    private void OnDestroy() => _destroy = true;

    private void Update() {
        if (_destroy) return;

        var currentTime = UnixTime.GetCurrentTime();

        if (currentTime < _nextCheck) return;

        var localPlayer = StartOfRound.Instance.localPlayerController;

        var localPlayerPosition = localPlayer.transform.position;

        var poisonPosition = gameObject.transform.position;

        var distance = Vector3.Distance(poisonPosition, localPlayerPosition);

        if (distance > 8) {
            _nextCheck = currentTime + 100;
            return;
        }

        localPlayer.DamagePlayer(_random.Next(3, 9), causeOfDeath: CauseOfDeath.Suffocation);

        _nextCheck = currentTime + 500;

        PlayCoughAudio(currentTime);
    }

    private void PlayCoughAudio(long currentTime) {
        if (_instanceId is not null && FindObjectFromInstanceID(_instanceId.Value) is not null) return;

        var audioObject = new GameObject("TemporaryCoughAudio");
        _instanceId = audioObject.GetInstanceID();

        if (PoisonPuffer.coughAudioClips is null) {
            _nextCheck = currentTime + 1000;
            return;
        }

        var soundIndex = _random.Next(0, PoisonPuffer.coughAudioClips.Count);

        if (_PreviousCoughs.Contains(soundIndex)) {
            _nextCheck = currentTime + 100;
            return;
        }

        if (_PreviousCoughs.Count >= 3) _PreviousCoughs.RemoveAt(0);

        _PreviousCoughs.Add(soundIndex);

        var coughAudio = PoisonPuffer.coughAudioClips[soundIndex];

        if (coughAudio is null) {
            _nextCheck = currentTime + 1000;
            return;
        }

        PoisonPuffer.Logger.LogDebug($"Playing clip '{coughAudio.name}' ({soundIndex})");

        var audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = coughAudio;
        audioSource.volume = 1F;
        audioSource.Play();

        Destroy(audioObject, coughAudio.length);
    }
}