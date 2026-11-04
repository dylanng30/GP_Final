using System.Collections.Generic;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Events;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Điểm vào duy nhất để gây sát thương, hồi máu và áp trạng thái.
    /// Event phát ra từ đây luôn chứa kết quả cuối cùng.
    /// </summary>
    public sealed class CombatResolver : ServiceBase
    {
        private readonly CombatRulesSO _rules;
        private readonly DamagePipeline _damagePipeline;
        private readonly List<ReactionRuleSO> _matchedReactions = new List<ReactionRuleSO>();

        public CombatResolver(CombatRulesSO rules)
        {
            _rules = rules;
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
            if (!TryGetLivingTarget(target, out ITargetable targetable) || targetable.IsInvulnerable)
            {
                return DamageResult.None;
            }

            StatController sourceStats = GetStats(source);
            StatController targetStats = targetable.Stats;
            ITargetable sourceActor = source != null ? source.GetComponent<ITargetable>() : null;

            var request = new DamageRequest(
                amount,
                element,
                targetable.Element,
                sourceStats != null ? sourceStats.GetStatValue(StatType.DamageBonus) : 0f,
                sourceStats != null ? sourceStats.CritRate : 0f,
                sourceStats != null ? sourceStats.CritDamage : 0f,
                targetStats != null ? targetStats.DEF : 0f,
                canCrit,
                sourceActor != null ? sourceActor.Status.Set.GetOutgoingMultiplier(element) : 1f);

            DamageResult result = _damagePipeline.Run(request);

            targetable.Health.TakeDamage(result.Amount);

            EventBus.Publish(new DamageDealtEvent(
                source, targetable.Transform.gameObject, result.Amount, element, hitPoint, result.IsCritical, result.Relation));

            return result;
        }

        /// <summary>Hồi máu có tính HealingBonus của nguồn và trả về lượng máu thực nhận.</summary>
        public float Heal(GameObject source, GameObject target, float amount)
        {
            if (!TryGetLivingTarget(target, out ITargetable targetable)) return 0f;

            StatController sourceStats = GetStats(source);
            float bonus = sourceStats != null ? sourceStats.GetStatValue(StatType.HealingBonus) : 0f;

            float healed = targetable.Health.Heal(amount * (1f + bonus));
            if (healed > 0f)
            {
                EventBus.Publish(new HealAppliedEvent(source, targetable.Transform.gameObject, healed));
            }
            return healed;
        }

        /// <summary>Áp trạng thái lên mục tiêu, kích hoạt tương khắc nếu có.</summary>
        public bool ApplyStatus(GameObject source, GameObject target, StatusDefinitionSO status)
        {
            if (!TryGetLivingTarget(target, out ITargetable targetable) || targetable.IsInvulnerable) return false;

            ResolveReactions(source, targetable, status);

            StatController sourceStats = GetStats(source);
            float attack = sourceStats != null ? sourceStats.ATK : 0f;

            float duration = status.Duration;
            if (status.IsControl && sourceStats != null)
            {
                duration *= 1f + sourceStats.GetStatValue(StatType.ControlDurationBonus);
            }

            float tickPower = status.TickDamage + attack * status.TickAttackRatio;

            targetable.Status.Set.Apply(status, duration, tickPower, source);
            EventBus.Publish(new StatusAppliedEvent(source, targetable.Transform.gameObject, status));
            return true;
        }

        private void ResolveReactions(GameObject source, ITargetable target, StatusDefinitionSO incoming)
        {
            ReactionTable.FindMatches(_rules.ReactionRules, incoming, target.Status.Set, _matchedReactions);

            foreach (ReactionRuleSO rule in _matchedReactions)
            {
                if (rule.RemovesRequiredStatus) target.Status.Set.Remove(rule.RequiredOnTarget);

                EventBus.Publish(new ReactionTriggeredEvent(source, target.Transform.gameObject, rule));

                StatController sourceStats = GetStats(source);
                if (rule.BonusAttackRatio <= 0f || sourceStats == null) continue;

                Deal(source, target.Transform.gameObject, sourceStats.ATK * rule.BonusAttackRatio,
                    incoming.Element, target.Transform.position, canCrit: false);
            }
        }

        private static bool TryGetLivingTarget(GameObject target, out ITargetable targetable)
        {
            targetable = target != null ? target.GetComponentInParent<ITargetable>() : null;
            return targetable != null && !targetable.IsDead;
        }

        private static StatController GetStats(GameObject source)
        {
            return source != null ? source.GetComponent<StatController>() : null;
        }
    }
}
