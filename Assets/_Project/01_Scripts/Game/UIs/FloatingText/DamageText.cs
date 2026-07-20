using UnityEngine;
using TMPro;
using DG.Tweening;
using Dylanng.Core.Pooling;
using Dylanng.Core;
using Dylanng.Core.Systems.TickSystem;

namespace Game.UIs.FloatingText
{
    public class DamageText : PoolableObject, ILateUpdatable
    {
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float aliveTime = 1f;

        public override void OnSpawn()
        {
            base.OnSpawn();
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Register(this);
            }
        }

        public override void OnDespawn()
        {
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
            }
            base.OnDespawn();
        }

        public void Setup(float damageAmount, Color color)
        {
            if (damageText != null)
            {
                damageText.text = $"-{Mathf.RoundToInt(damageAmount).ToString()}";
                damageText.color = color;
                
                // Reset alpha
                Color c = damageText.color;
                c.a = 1f;
                damageText.color = c;
            }

            AnimateAndDespawn();
        }

        private void AnimateAndDespawn()
        {
            // Move upward
            transform.DOMoveY(transform.position.y + floatSpeed, aliveTime).SetEase(Ease.OutCubic);
 
            // Scale up slightly on spawn
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
 
            // Fade out near the end of lifetime
            if (damageText != null)
            {
                damageText.DOFade(0f, fadeDuration).SetDelay(aliveTime - fadeDuration);
            }
 
            // Return to pool when finished
            DOVirtual.DelayedCall(aliveTime, () =>
            {
                ReturnToPool();
            });
        }

        private void ReturnToPool()
        {
            var poolManager = Dylanng.Core.ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                poolManager.Despawn("DamageText", this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnLateUpdate(float deltaTime)
        {
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            transform.DOKill();
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
            }
        }
    }
}
