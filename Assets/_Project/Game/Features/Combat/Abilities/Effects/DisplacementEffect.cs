using Dylanng;
using System;
using System.Collections;
using Game.Combat;
using Game.Combat.Domain;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    public enum DisplacementType
    {
        Knockback,
        Pull,
        Knockup
    }

    [Serializable]
    public class DisplacementEffect : IAbilityEffect
    {
        private const float MinimumPullGap = 1.5f;

        public DisplacementType Type = DisplacementType.Knockback;
        public float Distance = 3f;
        public float Duration = 0.3f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0f, 1f)] public float Chance = 1.0f;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, _) = context;
            if (target == null || caster == null || UnityEngine.Random.value > Chance) return;

            var targetable = target.GetComponentInParent<ITargetable>();
            if (targetable is MonoBehaviour owner && owner.isActiveAndEnabled && !targetable.IsDead)
            {
                owner.StartCoroutine(DisplaceRoutine(caster, targetable));
            }
        }

        private IEnumerator DisplaceRoutine(GameObject caster, ITargetable targetable)
        {
            Transform target = targetable.Transform;
            object controlLock = targetable.Status.AddLock(ControlFlags.CannotMove | ControlFlags.CannotAct);
            target.GetComponent<IEntityController>()?.StopMove();

            var characterController = target.GetComponent<CharacterController>();
            var body = target.GetComponent<Rigidbody>();

            Vector3 start = target.position;
            Vector3 direction = ResolveDirection(caster.transform.position, start);
            float distance = ResolveDistance(caster.transform.position, start);

            float elapsed = 0f;
            Vector3 previous = start;

            while (elapsed < Duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Duration);

                Vector3 current = Type == DisplacementType.Knockup
                    ? start + Vector3.up * (Mathf.Sin(t * Mathf.PI) * distance)
                    : start + direction * (distance * Curve.Evaluate(t));

                MoveTarget(target, characterController, body, current - previous);
                previous = current;

                yield return null;
            }

            targetable.Status.RemoveLock(controlLock);
        }

        private Vector3 ResolveDirection(Vector3 casterPosition, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - casterPosition;
            toTarget.y = 0f;
            return (Type == DisplacementType.Pull ? -toTarget : toTarget).normalized;
        }

        private float ResolveDistance(Vector3 casterPosition, Vector3 targetPosition)
        {
            if (Type != DisplacementType.Pull) return Distance;

            float maxPull = Vector3.Distance(casterPosition, targetPosition) - MinimumPullGap;
            return Mathf.Clamp(Distance, 0f, Mathf.Max(0f, maxPull));
        }

        private static void MoveTarget(Transform target, CharacterController controller, Rigidbody body, Vector3 delta)
        {
            if (controller != null && controller.enabled)
            {
                controller.Move(delta);
            }
            else if (body != null && !body.isKinematic)
            {
                body.MovePosition(body.position + delta);
            }
            else
            {
                target.position += delta;
            }
        }
    }
}
