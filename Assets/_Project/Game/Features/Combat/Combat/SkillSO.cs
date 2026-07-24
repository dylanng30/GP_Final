using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Combat/Skill")]
    public class SkillSO : ScriptableData
    {
        public string SkillID;
        public string SkillName;
        [TextArea] public string Description;
        public float DamageMultiplier = 1.0f;
        public float ManaCost = 10f;
        public float Cooldown = 2f;
        public Sprite Icon;
    }
}
