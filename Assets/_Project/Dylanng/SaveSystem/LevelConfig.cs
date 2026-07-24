using System.Collections.Generic;
using UnityEngine;

namespace Dylanng
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Dylanng/Data/Level Config")]
    public class LevelConfig : ScriptableData
    {
        public List<LevelData> Levels;

        [Header("Loop Settings (Hyper Casual)")]
        [Tooltip("N?u b?t, khi ngu?i choi vu?t qua level cu?i, game s? l?p l?i c�c level cu.")]
        public bool LoopLevels = true;
        [Tooltip("Index b?t d?u l?p. V� d?: b? qua c�c level hu?ng d?n (0, 1) v� b?t d?u l?p t? level 2.")]
        public int LoopStartIndex = 0;
    }
}