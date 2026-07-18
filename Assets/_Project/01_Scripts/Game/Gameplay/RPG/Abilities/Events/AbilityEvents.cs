using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Abilities;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Events
{
    public struct SpawnVFXEvent : IEvent
    {
        public string VFXKey;
        public Vector3 Position;
        public Quaternion Rotation;
        public float AutoDespawnTime;

        public SpawnVFXEvent(string vfxKey, Vector3 position, Quaternion rotation, float autoDespawnTime = 3f)
        {
            VFXKey = vfxKey;
            Position = position;
            Rotation = rotation;
            AutoDespawnTime = autoDespawnTime;
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
        public ElementType Element;
        public Vector3 HitPoint;

        public DamageDealtEvent(GameObject caster, GameObject target, float damage, ElementType element, Vector3 hitPoint)
        {
            Caster = caster;
            Target = target;
            Damage = damage;
            Element = element;
            HitPoint = hitPoint;
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
