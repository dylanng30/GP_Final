using Game.Gameplay.Entities;
using Game.Combat.Domain;
using Game.Combat;
using Game.Data.Hero;
using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Controllers;
using Dylanng.Gameplay.RPG.Stats;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class LeapStrikeEffect : IAbilityEffect
    {
        public float LeapDuration = 0.5f;
        public float LeapHeight = 3f;
        public AnimationCurve JumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float DamageMultiplier = 1.5f;
        public float StopDistance = 1.5f;
        public GameElement Element = GameElement.Physical;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (caster == null) return;
            var mono = caster.GetComponent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(LeapCoroutine(caster, target, point, context.ResolveElement(Element)));
            }
        }

        private IEnumerator LeapCoroutine(GameObject caster, GameObject target, Vector3 point, GameElement element)
        {
            var cc = caster.GetComponent<CharacterController>();
            if (cc == null) yield break;

            var entityController = caster.GetComponent<IEntityController>();
            
            var controlTarget = caster.GetComponent<ITargetable>();
            object controlLock = controlTarget?.Status.AddLock(ControlFlags.CannotMove | ControlFlags.CannotAct);
            entityController?.StopMove();

            Vector3 startPos = caster.transform.position;
            Vector3 targetPos = point;

            if (target != null)
            {
                targetPos = target.transform.position;
            }

            Vector3 dashDirection = (targetPos - startPos);
            dashDirection.y = 0;
            float totalDistance = dashDirection.magnitude;
            
            // Stop slightly before the target
            if (totalDistance > StopDistance)
            {
                totalDistance -= StopDistance;
            }
            else
            {
                totalDistance = 0f;
            }

            dashDirection.Normalize();

            float elapsedTime = 0f;
            Vector3 previousLerpPos = startPos;

            while (elapsedTime < LeapDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / LeapDuration;
                
                // Horizontal movement (Linear or ease)
                float horizontalT = JumpCurve.Evaluate(t);
                Vector3 currentHorizontalPos = startPos + dashDirection * (totalDistance * horizontalT);
                
                // Vertical movement (Parabola)
                float verticalOffset = Mathf.Sin(t * Mathf.PI) * LeapHeight;
                Vector3 currentLerpPos = new Vector3(currentHorizontalPos.x, startPos.y + verticalOffset, currentHorizontalPos.z);
                
                Vector3 moveDelta = currentLerpPos - previousLerpPos;
                
                cc.Move(moveDelta);
                previousLerpPos = currentLerpPos;

                yield return null;
            }

            // Deal Damage upon landing
            if (target != null && !target.GetComponent<Game.Gameplay.Entities.ITargetable>().IsDead)
            {
                float atk = 50f;
                var stats = caster.GetComponent<StatController>();
                if (stats != null) atk = stats.ATK;

                float finalDamage = atk * DamageMultiplier;
                
                ServiceLocator.Get<CombatResolver>()?.Deal(caster, target, finalDamage, element, target.transform.position);
            }

            // Restore controllers
            controlTarget?.Status.RemoveLock(controlLock);
        }
    }
}
