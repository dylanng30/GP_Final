using UnityEngine;

namespace Dylanng.Gameplay.RPG.Stats
{
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "RPG/Character Stats")]
    public class CharacterBaseStatsSO : ScriptableData
    {
        public float BaseHP = 100;
        public float BaseATK = 10;
        public float BaseDEF = 5;
        public float BaseSpeed = 5;
    }
}
