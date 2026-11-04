using System;
using System.Collections;
using Game.Combat;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class ShieldEffect : IAbilityEffect
    {
        public float ShieldAmount = 100f;
        public float Duration = 3f;
        public PoolableObject ShieldVFXPrefab;
        public bool ShieldSelf = false;

        public void Execute(in AbilityContext context)
        {
            if (context.Caster == null) return;

            GameObject shieldTarget = ShieldSelf || context.Target == null ? context.Caster : context.Target;
            var targetable = shieldTarget.GetComponentInParent<ITargetable>();
            if (targetable is MonoBehaviour owner && owner.isActiveAndEnabled)
            {
                owner.StartCoroutine(ShieldRoutine(targetable));
            }
        }

        private IEnumerator ShieldRoutine(ITargetable target)
        {
            // Mỗi lần dùng là một lớp khiên riêng, nên cần một source riêng.
            object shieldSource = new object();
            target.Health.Shields.Add(shieldSource, ShieldAmount);

            PoolableObject vfx = SpawnVfx(target.Transform, out string vfxKey);

            yield return new WaitForSeconds(Duration);

            target.Health.Shields.RemoveSource(shieldSource);

            if (vfx != null)
            {
                vfx.transform.SetParent(null);
                ServiceLocator.Get<PoolManager>()?.Despawn(vfxKey, vfx);
            }
        }

        private PoolableObject SpawnVfx(Transform target, out string vfxKey)
        {
            vfxKey = null;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (ShieldVFXPrefab == null || poolManager == null) return null;

            vfxKey = ShieldVFXPrefab.name;
            poolManager.CreatePool(vfxKey, ShieldVFXPrefab, 3);

            var vfx = poolManager.Spawn<PoolableObject>(vfxKey, target.position, Quaternion.identity);
            if (vfx != null)
            {
                vfx.transform.SetParent(target);
                vfx.transform.localPosition = Vector3.up;
            }
            return vfx;
        }
    }
}
