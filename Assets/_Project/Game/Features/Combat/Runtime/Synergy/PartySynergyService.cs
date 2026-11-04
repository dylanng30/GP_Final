using System.Collections.Generic;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Events;
using Game.Gameplay.Entities;
using Game.Managers.Party;

namespace Game.Combat
{
    /// <summary>Tính các liên kết tương sinh của đội và áp buff lên hero hệ được sinh.</summary>
    public sealed class PartySynergyService : ServiceBase
    {
        private readonly CombatRulesSO _rules;
        private readonly List<ElementSynergySO> _activeLinks = new List<ElementSynergySO>();
        private readonly List<GameElement> _partyElements = new List<GameElement>();

        public IReadOnlyList<ElementSynergySO> ActiveLinks => _activeLinks;

        public PartySynergyService(CombatRulesSO rules)
        {
            _rules = rules;
        }

        public override void Initialize()
        {
            ServiceLocator.Register<PartySynergyService>(this);
            EventBus.Subscribe<PartyHeroesSpawnedEvent>(OnPartyHeroesSpawned);
            EventBus.Subscribe<ActorDiedEvent>(OnActorDied);
        }

        private void OnPartyHeroesSpawned(PartyHeroesSpawnedEvent evt) => Refresh();

        private void OnActorDied(ActorDiedEvent evt) => Refresh();

        public void Refresh()
        {
            var party = ServiceLocator.Get<PartyCombatManager>();
            if (party == null) return;

            IReadOnlyList<HeroInstance> heroes = party.ActiveHeroes;

            ClearBuffs(heroes);
            CollectElements(heroes);

            SynergyEvaluator.FindActive(_partyElements, _rules.Synergies, _rules.MaxActiveLinks, _activeLinks);

            foreach (ElementSynergySO link in _activeLinks)
            {
                ApplyLink(link, heroes);
            }

            EventBus.Publish(new PartySynergyChangedEvent(_activeLinks));
        }

        /// <summary>Tính liên kết cho một đội hình giả định, dùng trên màn xếp đội.</summary>
        public void Preview(IReadOnlyList<GameElement> elements, List<ElementSynergySO> results)
        {
            SynergyEvaluator.FindActive(elements, _rules.Synergies, _rules.MaxActiveLinks, results);
        }

        private void ClearBuffs(IReadOnlyList<HeroInstance> heroes)
        {
            foreach (HeroInstance hero in heroes)
            {
                if (hero != null && hero.Stats != null) hero.Stats.RemoveAllModifiersFromSource(this);
            }
        }

        private void CollectElements(IReadOnlyList<HeroInstance> heroes)
        {
            _partyElements.Clear();
            foreach (HeroInstance hero in heroes)
            {
                if (hero != null && !hero.IsDead) _partyElements.Add(hero.Element);
            }
        }

        private void ApplyLink(ElementSynergySO link, IReadOnlyList<HeroInstance> heroes)
        {
            foreach (HeroInstance hero in heroes)
            {
                if (hero == null || hero.Stats == null || hero.Element != link.Target) continue;

                foreach (StatBuff buff in link.Buffs)
                {
                    hero.Stats.AddModifier(buff.StatType, new StatModifier(buff.Value, buff.ModifierType, this));
                }
            }
        }
    }
}
