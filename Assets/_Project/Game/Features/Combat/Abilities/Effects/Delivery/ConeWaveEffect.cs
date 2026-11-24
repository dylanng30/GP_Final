using System;
using System.Collections;
using System.Collections.Generic;
using Dylanng;
using Game.Gameplay.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>
    /// Sóng hình quạt lan từ người thi triển ra phía trước. Mục tiêu bị trúng khi mép sóng chạm tới,
    /// mỗi mục tiêu một lần. Hướng sóng chốt lúc thi triển.
    /// </summary>
    [Serializable]
    public class ConeWaveEffect : IAbilityEffect
    {
        private const float HalfAngleFactor = 0.5f;

        public float Range = 9f;
        [Range(10f, 180f)] public float ArcAngle = 60f;
        public float Speed = 20f;

        [Header("--- Hiển thị ---")]
        public ConeWaveVisual VisualPrefab;

        [Tooltip("Vệt chém bay theo mép sóng, trải đều trong góc quạt")]
        public GameObject SlashPrefab;
        [Min(0)] public int SlashCount = 3;
        public float SlashHeight = 1f;
        public float SlashScale = 1f;
        [Tooltip("Độ nghiêng của từng vệt chém quanh hướng bay, lần lượt theo thứ tự")]
        public float[] SlashRolls = { 25f, 0f, -25f };

        [Tooltip("Vệt chạy trên mặt đất theo hướng sóng, ví dụ tia điện trên sàn")]
        public GameObject FloorTrailPrefab;
        public float FloorTrailScale = 1f;

        [SerializeReference] public List<IAbilityEffect> HitEffects = new List<IAbilityEffect>();

        public void Execute(in AbilityContext context)
        {
            if (context.Caster == null || !context.Caster.TryGetComponent<MonoBehaviourBase>(out var owner)) return;

            Vector3 origin = context.Caster.transform.position;
            Vector3 direction = context.Direction;

            if (VisualPrefab != null)
            {
                Object.Instantiate(VisualPrefab).Play(origin, direction, Range, ArcAngle, Speed);
            }

            LaunchSlashes(origin, direction);
            LaunchFloorTrail(origin, direction);

            owner.StartCoroutine(RunWave(context, origin, direction));
        }

        private void LaunchSlashes(Vector3 origin, Vector3 direction)
        {
            if (SlashPrefab == null || SlashCount <= 0) return;

            float travelTime = Range / Mathf.Max(0.01f, Speed);
            for (int i = 0; i < SlashCount; i++)
            {
                float t = SlashCount == 1 ? 0.5f : (float)i / (SlashCount - 1);
                float yaw = Mathf.Lerp(-ArcAngle * 0.35f, ArcAngle * 0.35f, t);
                Vector3 heading = Quaternion.AngleAxis(yaw, Vector3.up) * direction;
                float roll = SlashRolls != null && SlashRolls.Length > 0 ? SlashRolls[i % SlashRolls.Length] : 0f;

                Quaternion rotation = Quaternion.LookRotation(heading) * Quaternion.Euler(-15f, 0f, roll) * SlashPrefab.transform.rotation;
                GameObject slash = VfxSpawner.Spawn(SlashPrefab, origin + Vector3.up * SlashHeight + heading * 0.6f, rotation);
                slash.transform.localScale = SlashPrefab.transform.localScale * SlashScale;
                StretchParticles(slash, travelTime);
                slash.AddComponent<LinearMover>().Launch(heading * Speed, travelTime, 0.4f);
            }
        }

        private void LaunchFloorTrail(Vector3 origin, Vector3 direction)
        {
            if (FloorTrailPrefab == null) return;

            float travelTime = Range / Mathf.Max(0.01f, Speed);
            GameObject trail = VfxSpawner.Spawn(FloorTrailPrefab, origin + Vector3.up * 0.05f, Quaternion.LookRotation(direction));
            trail.transform.localScale = FloorTrailPrefab.transform.localScale * FloorTrailScale;
            trail.AddComponent<LinearMover>().Launch(direction * Speed, travelTime, 1.2f);

            // Vệt sàn lặp vô hạn: ngừng phát khi sóng tới cuối để phần đã vẽ tắt dần.
            ParticleSystem[] systems = trail.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particles in systems)
            {
                // Unity không cho đổi duration khi hệ đang chạy.
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.duration = Mathf.Max(0.05f, travelTime);
            }
            foreach (ParticleSystem particles in systems) particles.Play(false);
        }

        /// <summary>Kéo dài vòng đời particle để vệt chém còn thấy được tới cuối tầm bay.</summary>
        private static void StretchParticles(GameObject vfx, float travelTime)
        {
            foreach (ParticleSystem particles in vfx.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = particles.main;
                float lifetime = main.startLifetime.constantMax;
                if (lifetime <= 0f || lifetime >= travelTime) continue;

                main.simulationSpeed = Mathf.Max(0.25f, lifetime / travelTime);
            }
        }

        private IEnumerator RunWave(AbilityContext context, Vector3 origin, Vector3 direction)
        {
            var struck = new List<ITargetable>();
            var candidates = new List<ITargetable>();
            float travelled = 0f;

            while (travelled < Range && context.Caster != null)
            {
                travelled = Mathf.Min(Range, travelled + Speed * Time.deltaTime);
                HitTargetsReached(context, origin, direction, travelled, struck, candidates);
                yield return null;
            }
        }

        private void HitTargetsReached(
            in AbilityContext context, Vector3 origin, Vector3 direction, float radius,
            List<ITargetable> struck, List<ITargetable> candidates)
        {
            TargetQuery.Collect(origin, radius, TargetQuery.GetEnemyMask(context.Caster), context.Caster.transform, candidates);

            foreach (ITargetable target in candidates)
            {
                if (struck.Contains(target) || !IsInsideCone(origin, direction, target.Transform.position)) continue;

                struck.Add(target);
                AbilityContext hit = context.WithHit(target.Transform.gameObject, target.Transform.position);
                foreach (IAbilityEffect effect in HitEffects)
                {
                    effect?.Execute(hit);
                }
            }
        }

        private bool IsInsideCone(Vector3 origin, Vector3 direction, Vector3 point)
        {
            Vector3 toPoint = point - origin;
            toPoint.y = 0f;
            return toPoint.sqrMagnitude < 0.001f || Vector3.Angle(direction, toPoint) <= ArcAngle * HalfAngleFactor;
        }
    }
}
