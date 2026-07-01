using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Core.Commands
{
    public class AttackCommand : CommandBase
    {
        /*private IDamageable _attacker;
        private IDamageable _target;
        private float _damage;

        public AttackCommand(IDamageable attacker, IDamageable target, float damage)
        {
            _attacker = attacker;
            _target = target;
            _damage = damage;
        }*/

        public override void Execute()
        {
            /*if (_target != null)
            {
                _target.TakeDamage(_damage);
                //Debug.Log($"{_attacker} attacked {_target} for {_damage} damage.");
            }*/
        }

        public override void Undo()
        {
            /*// Trong RPG, Undo sát thương thường là hồi máu, nhưng có thể phức tạp nếu target đã chết.
            if (_target != null)
            {
                _target.Heal(_damage);
            }*/
        }
    }
}
