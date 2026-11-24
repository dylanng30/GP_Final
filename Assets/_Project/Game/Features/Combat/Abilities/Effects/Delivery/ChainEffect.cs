using System;
using System.Collections;
using System.Collections.Generic;
using Dylanng;
using Game.Combat.Domain;
using Game.Gameplay.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>
    /// Đánh mục tiêu đầu tiên rồi nảy sang kẻ địch gần nhất chưa bị trúng, mỗi lần nảy yếu dần.
    /// Mỗi mục tiêu chỉ trúng một lần. HitEffects nhận Potency đã giảm qua context.
    /// </summary>
    [Serializable]
    public class ChainEffect : IAbilityEffect
    {
        [Header("--- Mục tiêu ---")]
        [Tooltip("Khi chưa chọn mục tiêu, tìm kẻ địch gần điểm ngắm nhất trong bán kính này")]
        public float FirstTargetRadius = 3f;
        [Min(1)] public int MaxTargets = 5;
        public float BounceRadius = 6.5f;

        [Header("--- Nhịp và sát thương ---")]
        public float BounceInterval = 0.1f;
        [Tooltip("Mỗi lần nảy giảm bấy nhiêu phần sát thương gốc")]
        [Range(0f, 1f)] public float FalloffPerBounce = 0.15f;
        [Range(0f, 1f)] public float MinimumPotency = 0.4f;

        [Header("--- Hiển thị ---")]
        public LightningArc ArcPrefab;
        public float ArcLifetime = 0.3f;
        public GameObject ImpactVfx;
        public float ImpactScale = 0.6f;
        public float ImpactLifetime = 1f;
        [Tooltip("Tên xương phát tia sét đầu tiên (ví dụ mũi kiếm). Trống thì lấy giữa thân người thi triển")]
        public string OriginPointName = "";
        [Tooltip("Key âm thanh phát ở mỗi lần nảy")]
        public string BounceSoundKey = "";

        [SerializeReference] public List<IAbilityEffect> HitEffects = new List<IAbilityEffect>();

        public void Execute(in AbilityContext context)
        {
            if (context.Caster == null || !context.Caster.TryGetComponent<MonoBehaviourBase>(out var owner)) return;

            ITargetable first = FindFirstTarget(context);
            if (first != null) owner.StartCoroutine(RunChain(context, first));
        }

        private IEnumerator RunChain(AbilityContext context, ITargetable first)
        {
            var struck = new List<ITargetable>();
            var candidates = new List<ITargetable>();
            ITargetable current = first;
            Vector3 from = GetOrigin(context.Caster);

            for (int hitIndex = 0; hitIndex < MaxTargets && current != null; hitIndex++)
            {
                Vector3 to = GetBodyCenter(current.Transform);
                ShowBolt(from, to);
                if (!string.IsNullOrEmpty(BounceSoundKey)) EventBus.Publish(new Events.PlayAudioEvent(BounceSoundKey, to));
                Strike(context, current, to, hitIndex);

                struck.Add(current);
                from = to;

                yield return new WaitForSeconds(BounceInterval);
                if (context.Caster == null) yield break;

                current = FindNextTarget(context.Caster, from, struck, candidates);
            }
        }

        private void Strike(in AbilityContext context, ITargetable target, Vector3 hitPoint, int hitIndex)
        {
            float potency = context.Potency * AbilityScaling.ChainPotency(hitIndex, FalloffPerBounce, MinimumPotency);
            AbilityContext hit = context.WithHit(target.Transform.gameObject, hitPoint).WithPotency(potency);

            foreach (IAbilityEffect effect in HitEffects)
            {
                effect?.Execute(hit);
            }
        }

        private ITargetable FindFirstTarget(in AbilityContext context)
        {
            if (context.Target != null)
            {
                var selected = context.Target.GetComponentInParent<ITargetable>();
                if (selected != null && !selected.IsDead) return selected;
            }

            return TargetQuery.FindNearest(
                context.Point, FirstTargetRadius, TargetQuery.GetEnemyMask(context.Caster), context.Caster.transform);
        }

        private ITargetable FindNextTarget(GameObject caster, Vector3 from, List<ITargetable> struck, List<ITargetable> candidates)
        {
            TargetQuery.Collect(from, BounceRadius, TargetQuery.GetEnemyMask(caster), caster.transform, candidates);

            ITargetable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (ITargetable candidate in candidates)
            {
                if (struck.Contains(candidate)) continue;

                float sqrDistance = (candidate.Transform.position - from).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
            return nearest;
        }

        private void ShowBolt(Vector3 from, Vector3 to)
        {
            if (ArcPrefab != null)
            {
                Object.Instantiate(ArcPrefab).Play(from, to, ArcLifetime);
            }

            if (ImpactVfx != null)
            {
                GameObject impact = VfxSpawner.Spawn(ImpactVfx, to, Quaternion.identity);
                impact.transform.localScale *= ImpactScale;
                Object.Destroy(impact, ImpactLifetime);
            }
        }

        private Vector3 GetOrigin(GameObject caster)
        {
            if (!string.IsNullOrEmpty(OriginPointName) && caster.TryGetComponent<HeroInstance>(out var hero))
            {
                Transform point = hero.GetBone(OriginPointName);
                if (point != null) return point.position;
            }
            return GetBodyCenter(caster.transform);
        }

        /// <summary>Tâm collider để tia sét đánh vào giữa người thay vì dưới chân.</summary>
        private static Vector3 GetBodyCenter(Transform actor)
        {
            Collider body = actor.GetComponentInChildren<Collider>();
            return body != null ? body.bounds.center : actor.position + Vector3.up;
        }
    }
}
