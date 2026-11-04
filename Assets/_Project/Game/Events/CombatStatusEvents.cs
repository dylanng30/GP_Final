using System.Collections.Generic;
using Dylanng;
using Game.Combat.Domain;
using UnityEngine;

namespace Game.Events
{
    public struct PartyHeroesSpawnedEvent : IEvent
    {
    }

    public struct PartySynergyChangedEvent : IEvent
    {
        public IReadOnlyList<ElementSynergySO> ActiveLinks;

        public PartySynergyChangedEvent(IReadOnlyList<ElementSynergySO> activeLinks)
        {
            ActiveLinks = activeLinks;
        }
    }

    public struct StatusAppliedEvent : IEvent
    {
        public GameObject Source;
        public GameObject Target;
        public StatusDefinitionSO Status;

        public StatusAppliedEvent(GameObject source, GameObject target, StatusDefinitionSO status)
        {
            Source = source;
            Target = target;
            Status = status;
        }
    }

    public struct StatusRemovedEvent : IEvent
    {
        public GameObject Target;
        public StatusDefinitionSO Status;

        public StatusRemovedEvent(GameObject target, StatusDefinitionSO status)
        {
            Target = target;
            Status = status;
        }
    }

    public struct ReactionTriggeredEvent : IEvent
    {
        public GameObject Source;
        public GameObject Target;
        public ReactionRuleSO Rule;

        public ReactionTriggeredEvent(GameObject source, GameObject target, ReactionRuleSO rule)
        {
            Source = source;
            Target = target;
            Rule = rule;
        }
    }
}
