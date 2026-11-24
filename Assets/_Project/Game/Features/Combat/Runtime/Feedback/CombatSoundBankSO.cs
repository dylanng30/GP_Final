using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Bảng âm thanh chiến đấu tra theo key. Gameplay chỉ phát PlayAudioEvent với key,
    /// CombatAudioPlayer chọn ngẫu nhiên một clip trong entry và phát tại vị trí.
    /// Asset nằm ở Resources/CombatFeedback/CombatSoundBank.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSoundBank", menuName = "Game/Combat/Combat Sound Bank")]
    public class CombatSoundBankSO : ScriptableObject
    {
        public const string ResourcePath = "CombatFeedback/CombatSoundBank";

        [Serializable]
        public class Entry
        {
            public string Key;
            public List<AudioClip> Clips = new List<AudioClip>();
            [Range(0f, 1f)] public float Volume = 0.8f;
            public float PitchMin = 0.95f;
            public float PitchMax = 1.05f;
            [Tooltip("Thời gian tối thiểu giữa hai lần phát cùng key, tránh chồng tiếng khi nhiều đòn trúng cùng lúc")]
            public float MinInterval = 0.04f;
        }

        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Tooltip("0 = 2D, 1 = 3D hoàn toàn")]
        [Range(0f, 1f)] public float SpatialBlend = 0.6f;
        public float MinDistance = 6f;
        public float MaxDistance = 40f;
        public List<Entry> Entries = new List<Entry>();

        public Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            foreach (Entry entry in Entries)
            {
                if (entry != null && entry.Key == key) return entry;
            }
            return null;
        }
    }
}
