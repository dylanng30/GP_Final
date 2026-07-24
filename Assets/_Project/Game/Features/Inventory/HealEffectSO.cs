using UnityEngine;

namespace Dylanng.Gameplay.RPG.Inventory
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "RPG/Inventory/Effects/Heal")]
    public class HealEffectSO : ScriptableEffect
    {
        public float Amount;

        public override void ExecuteEffect(GameObject target)
        {
            /*if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.Heal(Amount);
                Debug.Log($"[HealEffect] Healed {target.name} for {Amount}");
            }*/
        }
    }
}
