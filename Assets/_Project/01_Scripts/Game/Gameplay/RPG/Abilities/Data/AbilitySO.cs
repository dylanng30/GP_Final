using System.Collections.Generic;
using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Dylanng/RPG/Abilities/Ability Data")]
    public class AbilitySO : ScriptableData
    {
        [Header("--- Basic Info ---")]
        public string AbilityID;
        public string AbilityName;
        [TextArea(3, 5)] public string Description;
        public Sprite Icon;
        public AbilityType Type;
        public ElementType Element;

        [Header("--- Costs & Cooldown ---")]
        public float Cooldown = 4.0f;
        public float EnergyCost = 20.0f;
        public float StaminaCost = 0.0f;

        [Header("--- Animation & Timings ---")]
        public int AnimationTriggerHash;
        public float TotalCastDuration = 1.2f;

        [Header("--- Strategy Effects (Polymorphic) ---")]
        [SerializeReference] 
        public List<IAbilityEffect> Effects = new List<IAbilityEffect>();

        public void ExecuteEffects(GameObject caster, GameObject target, Vector3 point)
        {
            if (Effects == null) return;
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i]?.Execute(caster, target, point);
            }
        }
    }
}
