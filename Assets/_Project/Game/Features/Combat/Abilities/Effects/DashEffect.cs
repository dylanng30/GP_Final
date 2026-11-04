using Dylanng;
using Game.Gameplay.Entities;
using Game.Combat.Domain;
using Game.Combat;
using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Controllers;

namespace Game.Combat.Abilities.Effects
{
    [Serializable]
    public class DashEffect : IAbilityEffect
    {
        public float DashDistance = 5f;
        public float DashDuration = 0.2f;
        public AnimationCurve DashCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool StopAtTarget = false;
        public float StopDistance = 1.5f;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (caster == null) return;
            var mono = caster.GetComponent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(DashCoroutine(caster, target, point));
            }
        }

        private IEnumerator DashCoroutine(GameObject caster, GameObject target, Vector3 point)
        {
            var cc = caster.GetComponent<CharacterController>();
            if (cc == null) yield break;

            var entityController = caster.GetComponent<IEntityController>();
            
            // Disable AI and movement inputs temporarily
            
            var controlTarget = caster.GetComponent<ITargetable>();
            object controlLock = controlTarget?.Status.AddLock(ControlFlags.CannotMove | ControlFlags.CannotAct);
            entityController?.StopMove();

            Vector3 startPos = caster.transform.position;
            Vector3 dashDirection = (point - startPos);
            dashDirection.y = 0; // Dash horizontally

            if (dashDirection == Vector3.zero)
            {
                dashDirection = caster.transform.forward;
            }
            
            dashDirection.Normalize();

            float actualDistance = DashDistance;

            if (StopAtTarget && target != null)
            {
                float distToTarget = Vector3.Distance(startPos, target.transform.position);
                actualDistance = Mathf.Max(0, distToTarget - StopDistance);
                dashDirection = (target.transform.position - startPos).normalized;
            }

            float elapsedTime = 0f;
            Vector3 previousLerpPos = startPos;

            while (elapsedTime < DashDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / DashDuration;
                float curveValue = DashCurve.Evaluate(t);
                
                Vector3 currentLerpPos = startPos + dashDirection * (actualDistance * curveValue);
                Vector3 moveDelta = currentLerpPos - previousLerpPos;
                
                cc.Move(moveDelta);
                previousLerpPos = currentLerpPos;

                yield return null;
            }

            // Restore controllers
            controlTarget?.Status.RemoveLock(controlLock);
        }
    }
}
