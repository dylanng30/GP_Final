using System.Collections.Generic;
using Dylanng;
using Game.Combat.Abilities.Events;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>Phát âm thanh cho PlayAudioEvent bằng một pool AudioSource 3D.</summary>
    public class CombatAudioPlayer : MonoBehaviour
    {
        private const int PoolSize = 16;

        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private readonly Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();
        private CombatSoundBankSO _bank;
        private int _nextSource;

        private void Awake()
        {
            _bank = Resources.Load<CombatSoundBankSO>(CombatSoundBankSO.ResourcePath);
            if (_bank == null)
            {
                GameLogger.LogWarning($"[CombatAudioPlayer] Không tìm thấy Resources/{CombatSoundBankSO.ResourcePath}, sẽ không có âm thanh chiến đấu.");
                return;
            }

            var root = new GameObject("CombatAudioPool").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < PoolSize; i++)
            {
                var sourceObject = new GameObject("CombatAudio_" + i);
                sourceObject.transform.SetParent(root, false);
                var source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = _bank.SpatialBlend;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = _bank.MinDistance;
                source.maxDistance = _bank.MaxDistance;
                source.dopplerLevel = 0f;
                _sources.Add(source);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayAudioEvent>(OnPlayAudio);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayAudioEvent>(OnPlayAudio);
        }

        private void OnPlayAudio(PlayAudioEvent evt)
        {
            if (_bank == null || _sources.Count == 0) return;

            CombatSoundBankSO.Entry entry = _bank.Find(evt.AudioKey);
            if (entry == null || entry.Clips.Count == 0) return;

            float now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(evt.AudioKey, out float last) && now - last < entry.MinInterval) return;
            _lastPlayTime[evt.AudioKey] = now;

            AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Count)];
            if (clip == null) return;

            AudioSource source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Count;

            source.transform.position = evt.Position;
            source.pitch = Random.Range(entry.PitchMin, entry.PitchMax);
            source.PlayOneShot(clip, entry.Volume * _bank.MasterVolume);
        }
    }
}
