using Game.Combat.Abilities;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Thông tin một lần thi triển, truyền xuyên suốt các effect lồng nhau.</summary>
    public readonly struct AbilityContext
    {
        public readonly GameObject Caster;
        public readonly GameObject Target;
        public readonly Vector3 Point;
        public readonly GameElement Element;
        public readonly AbilitySO Ability;

        public AbilityContext(GameObject caster, GameObject target, Vector3 point, GameElement element, AbilitySO ability = null)
        {
            Caster = caster;
            Target = target;
            Point = point;
            Element = element;
            Ability = ability;
        }

        /// <summary>Hướng từ người thi triển đến điểm mục tiêu trên mặt phẳng ngang.</summary>
        public Vector3 Direction
        {
            get
            {
                if (Caster == null) return Vector3.forward;

                Vector3 direction = Point - Caster.transform.position;
                direction.y = 0f;
                return direction.sqrMagnitude > 0.001f ? direction.normalized : Caster.transform.forward;
            }
        }

        public AbilityContext WithHit(GameObject target, Vector3 point)
        {
            return new AbilityContext(Caster, target, point, Element, Ability);
        }

        /// <summary>Hệ của effect, Physical nghĩa là dùng hệ của skill.</summary>
        public GameElement ResolveElement(GameElement effectElement)
        {
            return effectElement != GameElement.Physical ? effectElement : Element;
        }

        public void Deconstruct(out GameObject caster, out GameObject target, out Vector3 point)
        {
            caster = Caster;
            target = Target;
            point = Point;
        }
    }
}
