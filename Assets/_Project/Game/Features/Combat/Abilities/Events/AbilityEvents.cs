using Dylanng;
using Game.Combat.Domain;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Abilities.Events
{
    public struct SpawnVFXEvent : IEvent
    {
        public string VFXKey;
        public Vector3 Position;
        public Quaternion Rotation;
        public float AutoDespawnTime;
        public float Radius;

        public SpawnVFXEvent(string vfxKey, Vector3 position, Quaternion rotation, float autoDespawnTime = 3f, float radius = 0f)
        {
            VFXKey = vfxKey;
            Position = position;
            Rotation = rotation;
            AutoDespawnTime = autoDespawnTime;
            Radius = radius;
        }
    }

    public struct PlayAudioEvent : IEvent
    {
        public string AudioKey;
        public Vector3 Position;

        public PlayAudioEvent(string audioKey, Vector3 position)
        {
            AudioKey = audioKey;
            Position = position;
        }
    }

    public struct DamageDealtEvent : IEvent
    {
        public GameObject Caster;
        public GameObject Target;
        public float Damage;
        public GameElement Element;
        public Vector3 HitPoint;
        public bool IsCritical;
        public ElementRelation Relation;

        /// <summary>Sát thương theo nhịp (DoT, vùng). Không gây hitstop hay đẩy lùi.</summary>
        public bool IsPeriodic;

        public DamageDealtEvent(GameObject caster, GameObject target, float damage, GameElement element, Vector3 hitPoint, bool isCritical = false, ElementRelation relation = ElementRelation.Neutral, bool isPeriodic = false)
        {
            Caster = caster;
            Target = target;
            Damage = damage;
            Element = element;
            HitPoint = hitPoint;
            IsCritical = isCritical;
            Relation = relation;
            IsPeriodic = isPeriodic;
        }
    }

    /// <summary>Báo trước vùng skill sắp đánh xuống, phát khi bắt đầu cast.</summary>
    public struct AbilityTelegraphEvent : IEvent
    {
        public GameObject Caster;
        public AbilitySO Ability;
        public Vector3 Point;
        public float Radius;
        public float Duration;

        public AbilityTelegraphEvent(GameObject caster, AbilitySO ability, Vector3 point, float radius, float duration)
        {
            Caster = caster;
            Ability = ability;
            Point = point;
            Radius = radius;
            Duration = duration;
        }
    }

    public struct HeroDashedEvent : IEvent
    {
        public GameObject Hero;
        public float Duration;

        public HeroDashedEvent(GameObject hero, float duration)
        {
            Hero = hero;
            Duration = duration;
        }
    }

    /// <summary>Bắt đầu một đòn đánh thường cận chiến (lúc animation đánh bắt đầu chạy).</summary>
    public struct MeleeAttackStartedEvent : IEvent
    {
        public GameObject Attacker;
        public GameElement Element;

        public MeleeAttackStartedEvent(GameObject attacker, GameElement element)
        {
            Attacker = attacker;
            Element = element;
        }
    }

    /// <summary>Một nhát chém cận chiến vừa vung ra, có trúng mục tiêu hay không.</summary>
    public struct MeleeSwingEvent : IEvent
    {
        public GameObject Attacker;
        public GameElement Element;
        public Game.Combat.Abilities.BasicAttack.ComboStep Step;
        public bool HasHit;

        public MeleeSwingEvent(GameObject attacker, GameElement element, Game.Combat.Abilities.BasicAttack.ComboStep step, bool hasHit)
        {
            Attacker = attacker;
            Element = element;
            Step = step;
            HasHit = hasHit;
        }
    }

    /// <summary>Một phát đánh thường tầm xa vừa rời nòng.</summary>
    public struct RangedShotEvent : IEvent
    {
        public GameObject Attacker;
        public Game.Combat.Abilities.BasicAttack.BasicAttackSO Config;
        public Vector3 Position;
        public Quaternion Rotation;

        /// <summary>Điểm bắn trên người (đầu nòng). Null nếu bắn từ offset cố định.</summary>
        public Transform Muzzle;

        public RangedShotEvent(GameObject attacker, Game.Combat.Abilities.BasicAttack.BasicAttackSO config, Vector3 position, Quaternion rotation, Transform muzzle)
        {
            Attacker = attacker;
            Config = config;
            Position = position;
            Rotation = rotation;
            Muzzle = muzzle;
        }
    }

    public struct HeroLandedEvent : IEvent
    {
        public GameObject Hero;

        public HeroLandedEvent(GameObject hero)
        {
            Hero = hero;
        }
    }

    public struct HealAppliedEvent : IEvent
    {
        public GameObject Source;
        public GameObject Target;
        public float Amount;

        public HealAppliedEvent(GameObject source, GameObject target, float amount)
        {
            Source = source;
            Target = target;
            Amount = amount;
        }
    }

    public struct ActorDiedEvent : IEvent
    {
        public GameObject Actor;

        public ActorDiedEvent(GameObject actor)
        {
            Actor = actor;
        }
    }

    public struct AbilityCastStartedEvent : IEvent
    {
        public GameObject Caster;
        public AbilitySO Ability;

        public AbilityCastStartedEvent(GameObject caster, AbilitySO ability)
        {
            Caster = caster;
            Ability = ability;
        }
    }

    public struct AbilityExecutedEvent : IEvent
    {
        public GameObject Caster;
        public AbilitySO Ability;
        public Vector3 Point;

        public AbilityExecutedEvent(GameObject caster, AbilitySO ability, Vector3 point)
        {
            Caster = caster;
            Ability = ability;
            Point = point;
        }
    }
}
