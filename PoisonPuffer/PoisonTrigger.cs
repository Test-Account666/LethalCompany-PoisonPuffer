using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace PoisonPuffer;

public class PoisonTrigger : MonoBehaviour {
    private readonly Random _random = new();
    private int? _instanceId;
    private static readonly List<int> _PreviousCoughs = new(3);

    private void Start() {
        StartCoroutine(CheckForPlayers());
    }

    private IEnumerator CheckForPlayers() {
        while (true) {
            var localPlayer = StartOfRound.Instance.localPlayerController;

            var localPlayerPosition = localPlayer.transform.position;

            var poisonPosition = gameObject.transform.position;

            var distance = Vector3.Distance(poisonPosition, localPlayerPosition);

            if (distance > 8) {
                yield return new WaitForEndOfFrame();
                continue;
            }

            localPlayer.DamagePlayer(_random.Next(3, 9), causeOfDeath: CauseOfDeath.Suffocation);

            if (_instanceId is null || FindObjectFromInstanceID(_instanceId.Value) is null) {
                var audioObject = new GameObject("TemporaryCoughAudio");
                _instanceId = audioObject.GetInstanceID();

                if (PoisonPuffer.coughAudioClips is null) {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                var soundIndex = _random.Next(0, PoisonPuffer.coughAudioClips.Count);

                while (_PreviousCoughs.Contains(soundIndex)) {
                    soundIndex = _random.Next(0, PoisonPuffer.coughAudioClips.Count);
                    yield return new WaitForEndOfFrame();
                }

                if (_PreviousCoughs.Count >= 3)
                    _PreviousCoughs.RemoveAt(0);

                _PreviousCoughs.Add(soundIndex);

                var coughAudio = PoisonPuffer.coughAudioClips[soundIndex];

                if (coughAudio is null) {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                PoisonPuffer.Logger.LogDebug($"Playing clip '{coughAudio.name}' ({soundIndex})");

                var audioSource = audioObject.AddComponent<AudioSource>();
                audioSource.clip = coughAudio;
                audioSource.volume = 1F;
                audioSource.Play();

                Destroy(audioObject, coughAudio.length);
            }

            yield return new WaitForSeconds(1);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}