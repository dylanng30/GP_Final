using Game.Combat;
using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Controllers;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
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
        public DisplacementType Type = DisplacementType.Knockback;
        public float Distance = 3f;
        public float Duration = 0.3f;
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0f, 1f)] public float Chance = 1.0f;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (target == null || caster == null || UnityEngine.Random.value > Chance) return;

            var mono = target.GetComponent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(DisplaceCoroutine(caster, target));
            }
        }

        private IEnumerator DisplaceCoroutine(GameObject caster, GameObject target)
        {
            // Temporarily disable AI/Input if it's a hero or intelligent entity
            var aiController = target.GetComponent<HeroAIController>();
            var inputController = target.GetComponent<PlayerInputController>();
            var entityController = target.GetComponent<IEntityController>();
            
            if (aiController != null) aiController.enabled = false;
            if (inputController != null) inputController.enabled = false;
            entityController?.StopMove();

            var cc = target.GetComponent<CharacterController>();
            var rb = target.GetComponent<Rigidbody>();

            Vector3 startPos = target.transform.position;
            Vector3 direction = Vector3.zero;

            if (Type == DisplacementType.Knockback)
            {
                direction = (target.transform.position - caster.transform.position);
                direction.y = 0;
                direction.Normalize();
            }
            else if (Type == DisplacementType.Pull)
            {
                direction = (caster.transform.position - target.transform.position);
                direction.y = 0;
                direction.Normalize();
                
                // Adjust distance so we don't pull them inside the caster
                float distToCaster = Vector3.Distance(target.transform.position, caster.transform.position);
                if (Distance > distToCaster - 1.5f)
                {
                    Distance = Mathf.Max(0, distToCaster - 1.5f);
                }
            }
            else if (Type == DisplacementType.Knockup)
            {
                direction = Vector3.up;
            }

            float elapsedTime = 0f;
            Vector3 previousLerpPos = startPos;

            while (elapsedTime < Duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / Duration;
                
                float curveValue = Curve.Evaluate(t);
                
                Vector3 currentLerpPos;

                if (Type == DisplacementType.Knockup)
                {
                    // Parabola for knockup
                    float verticalOffset = Mathf.Sin(t * Mathf.PI) * Distance;
                    currentLerpPos = new Vector3(startPos.x, startPos.y + verticalOffset, startPos.z);
                }
                else
                {
                    currentLerpPos = startPos + direction * (Distance * curveValue);
                }
                
                Vector3 moveDelta = currentLerpPos - previousLerpPos;
                
                if (cc != null)
                {
                    cc.Move(moveDelta);
                }
                else if (rb != null && !rb.isKinematic)
                {
                    rb.MovePosition(rb.position + moveDelta);
                }
                else
                {
                    target.transform.position += moveDelta;
                }

                previousLerpPos = currentLerpPos;

                yield return null;
            }

            // Restore controllers
            if (aiController != null) aiController.enabled = true;
            if (inputController != null) inputController.enabled = true;
        }
    }
}
