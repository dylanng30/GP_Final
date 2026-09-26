using System.Collections.Generic;
using UnityEngine;
using Game.Data.Hero;
using Dylanng.Gameplay.RPG.Stats;
using Dylanng;
using Game.Events;

namespace Game.Managers.Party
{
    public struct PartyHeroesSpawnedEvent : IEvent { }

    public class PartySynergyManager : ManagerBase
    {
        [SerializeField] private List<SynergyConfigSO> _availableSynergies = new List<SynergyConfigSO>();

        private PartyCombatManager _combatManager;

        public override void Initialize()
        {
            ServiceLocator.Register<PartySynergyManager>(this);

            EventBus.Subscribe<PartyHeroesSpawnedEvent>(OnPartyHeroesSpawned);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<PartyHeroesSpawnedEvent>(OnPartyHeroesSpawned);
            
            RemoveAllSynergyBuffs();

            ServiceLocator.Unregister<PartySynergyManager>();
            base.OnDestroy();
        }

        private void OnPartyHeroesSpawned(PartyHeroesSpawnedEvent evt)
        {
            CheckAndApplySynergy();
        }

        public void CheckAndApplySynergy()
        {
            RemoveAllSynergyBuffs();

            if (_combatManager == null)
                _combatManager = ServiceLocator.Get<PartyCombatManager>();

            if (_combatManager == null || _combatManager.ActiveHeroes == null) return;

            // Đếm số lượng nguyên tố
            Dictionary<GameElement, int> elementCounts = new Dictionary<GameElement, int>();
            
            foreach (var hero in _combatManager.ActiveHeroes)
            {
                if (hero != null && !hero.IsDead)
                {
                    if (!elementCounts.ContainsKey(hero.HeroElement))
                        elementCounts[hero.HeroElement] = 0;
                    
                    elementCounts[hero.HeroElement]++;
                }
            }

            // Áp dụng buff
            foreach (var config in _availableSynergies)
            {
                if (config == null) continue;

                if (elementCounts.TryGetValue(config.RequiredElement, out int count) && count >= config.RequiredCount)
                {
                    ApplySynergyBuff(config);
                }
            }
        }

        private void ApplySynergyBuff(SynergyConfigSO config)
        {
            if (_combatManager == null || _combatManager.ActiveHeroes == null) return;

            foreach (var hero in _combatManager.ActiveHeroes)
            {
                if (hero != null && hero.Stats != null)
                {
                    foreach (var buff in config.Buffs)
                    {
                        var modifier = new StatModifier(buff.Value, buff.ModifierType, this);
                        hero.Stats.AddModifier(buff.StatType, modifier);
                    }
                    Debug.Log($"[PartySynergy] Áp dụng Cộng Hưởng {config.RequiredElement} cho {hero.HeroId}");
                }
            }
        }

        private void RemoveAllSynergyBuffs()
        {
            if (_combatManager == null || _combatManager.ActiveHeroes == null) return;

            foreach (var hero in _combatManager.ActiveHeroes)
            {
                if (hero != null && hero.Stats != null)
                {
                    hero.Stats.RemoveAllModifiersFromSource(this);
                }
            }
        }
    }
}
