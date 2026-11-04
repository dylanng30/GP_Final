using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Điểm vào duy nhất để gây sát thương: tính qua DamagePipeline rồi phát DamageDealtEvent.
    /// Vì vậy event luôn chứa số sát thương cuối cùng.
    /// </summary>
    public sealed class CombatResolver : ServiceBase
    {
        private readonly DamagePipeline _damagePipeline;

        public CombatResolver(CombatRulesSO rules)
        {
            _damagePipeline = rules.CreateDamagePipeline(new UnityRandomSource());
        }

        public override void Initialize()
        {
            ServiceLocator.Register<CombatResolver>(this);
        }

        public DamageResult Deal(
            GameObject source,
            GameObject target,
            float amount,
            GameElement element,
            Vector3 hitPoint,
            bool canCrit = true)
        {
            if (target == null) return DamageResult.None;

            var targetable = target.GetComponentInParent<ITargetable>();
            if (targetable == null || targetable.IsDead || targetable.IsInvulnerable) return DamageResult.None;

            StatController sourceStats = source != null ? source.GetComponent<StatController>() : null;
            StatController targetStats = targetable.Stats;

            var request = new DamageRequest(
                amount,
                element,
                targetable.Element,
                sourceStats != null ? sourceStats.GetStatValue(StatType.DamageBonus) : 0f,
                sourceStats != null ? sourceStats.CritRate : 0f,
                sourceStats != null ? sourceStats.CritDamage : 0f,
                targetStats != null ? targetStats.DEF : 0f,
                canCrit);

            DamageResult result = _damagePipeline.Run(request);

            targetable.Health.TakeDamage(result.Amount);

            EventBus.Publish(new DamageDealtEvent(
                source, targetable.Transform.gameObject, result.Amount, element, hitPoint, result.IsCritical, result.Relation));

            return result;
        }

        /// <summary>Hồi máu có tính HealingBonus của nguồn và trả về lượng máu thực nhận.</summary>
        public float Heal(GameObject source, GameObject target, float amount)
        {
            if (target == null) return 0f;

            var targetable = target.GetComponentInParent<ITargetable>();
            if (targetable == null) return 0f;

            StatController sourceStats = source != null ? source.GetComponent<StatController>() : null;
            float bonus = sourceStats != null ? sourceStats.GetStatValue(StatType.HealingBonus) : 0f;

            float healed = targetable.Health.Heal(amount * (1f + bonus));
            if (healed > 0f)
            {
                EventBus.Publish(new HealAppliedEvent(source, targetable.Transform.gameObject, healed));
            }
            return healed;
        }
    }
}
