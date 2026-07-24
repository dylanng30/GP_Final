using System.Collections;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.VFXs;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Managers
{
    public class AbilityVFXManager : ManagerBase
    {
        public override void Initialize()
        {
            EventBus.Subscribe<SpawnVFXEvent>(OnSpawnVFX);
            EventBus.Subscribe<PlayAudioEvent>(OnPlayAudio);
            ServiceLocator.Register<AbilityVFXManager>(this, ServiceScope.Global);
        }

        private void OnSpawnVFX(SpawnVFXEvent evt)
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                var vfxObj = poolManager.Spawn<PoolableObject>(evt.VFXKey, evt.Position, evt.Rotation);
                if (vfxObj != null)
                {
                    if (evt.Radius > 0f)
                    {
                        vfxObj.transform.localScale = Vector3.one * evt.Radius;
                    }
                    else
                    {
                        vfxObj.transform.localScale = Vector3.one;
                    }

                    if (vfxObj.TryGetComponent<BaseVFXView>(out var vfxView))
                    {
                        vfxView.Play(evt.Position, (view) =>
                        {
                            poolManager.Despawn(evt.VFXKey, view);
                        });
                    }
                    else if (evt.AutoDespawnTime > 0)
                    {
                        StartCoroutine(AutoDespawnVFXCoroutine(evt.VFXKey, vfxObj, evt.AutoDespawnTime));
                    }
                }
            }
        }

        private void OnPlayAudio(PlayAudioEvent evt)
        {
            Debug.Log($"[AbilityVFXManager] Phát âm thanh '{evt.AudioKey}' tại {evt.Position}");
        }

        private IEnumerator AutoDespawnVFXCoroutine(string poolKey, PoolableObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null && obj != null)
            {
                poolManager.Despawn(poolKey, obj);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus.Unsubscribe<SpawnVFXEvent>(OnSpawnVFX);
            EventBus.Unsubscribe<PlayAudioEvent>(OnPlayAudio);
        }
    }
}
