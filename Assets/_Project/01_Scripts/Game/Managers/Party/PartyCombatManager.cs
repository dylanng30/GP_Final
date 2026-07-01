using System.Collections.Generic;
using Dylanng.Core;
using Dylanng.Core.Base;
using Game.Events;
using Game.Gameplay.Entities;
using Game.Managers.Hero;
using UnityEngine;

namespace Game.Managers.Party
{
    public class PartyCombatManager : ManagerBase
    {
        private List<HeroInstance> _activeHeroes = new List<HeroInstance>();
        private int _currentActiveHeroIndex = 0;

        public override void Initialize()
        {
            ServiceLocator.Register<PartyCombatManager>(this);

            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null)
            {
                partyManager.OnPartyChanged += OnPartyChanged;
                SyncHeroes(partyManager.HeroIds);
            }

            InitializeCombat();
        }

        protected override void OnDestroy()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null)
            {
                partyManager.OnPartyChanged -= OnPartyChanged;
            }

            ClearHeroes();

            base.OnDestroy();
            
            ServiceLocator.Unregister<PartyCombatManager>();
        }

        private void OnPartyChanged()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null)
            {
                SyncHeroes(partyManager.HeroIds);
                InitializeCombat();
            }
        }

        private void ClearHeroes()
        {
            foreach (var hero in _activeHeroes)
            {
                if (hero != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(hero.gameObject);
                    }
                    else
#endif
                    {
                        Destroy(hero.gameObject);
                    }
                }
            }
            _activeHeroes.Clear();
        }

        private void SyncHeroes(string[] heroIds)
        {
            ClearHeroes();

            if (heroIds == null) return;

            var heroDataManager = ServiceLocator.Get<HeroDataManager>();

            for (int i = 0; i < heroIds.Length; i++)
            {
                string heroId = heroIds[i];
                if (!string.IsNullOrEmpty(heroId))
                {
                    HeroInstance prefabToSpawn = heroDataManager.GetHeroModel(heroId);
                    
                    if (prefabToSpawn != null)
                    {
                        HeroInstance hero = Instantiate(prefabToSpawn);
                        hero.SetWorldPosition(Vector3.right * (i - 2) * 2 + Vector3.up);
                        hero.Setup(heroId);
                        _activeHeroes.Add(hero);
                    }
                }
            }
        }

        public void InitializeCombat()
        {
            SwitchActiveHero(0);
        }

        public void SwitchActiveHero(int index)
        {
            Debug.Log($"SwitchActiveHero {index}");
            if (index < 0 || index >= _activeHeroes.Count) return;
            if (_activeHeroes.Count == 0) return;

            // Deactivate current hero
            if (_currentActiveHeroIndex >= 0 && _currentActiveHeroIndex < _activeHeroes.Count)
            {
                _activeHeroes[_currentActiveHeroIndex].SetAsActive(false);
            }

            // Activate new hero
            _currentActiveHeroIndex = index;
            _activeHeroes[_currentActiveHeroIndex].SetAsActive(true);

            // Notify UI & Camera
            EventBus.Publish(new ActiveHeroChangedEvent(_currentActiveHeroIndex, _activeHeroes[_currentActiveHeroIndex].HeroId));
            
            GameLogger.Log($"Switched Active Hero to index {_currentActiveHeroIndex}");
        }

        public void CleanupCombat()
        {
            foreach (var hero in _activeHeroes)
            {
                if (hero != null)
                {
                    hero.SetAsActive(false);
                }
            }
            _currentActiveHeroIndex = -1;
        }
        
        public HeroInstance GetActiveHero()
        {
            if (_currentActiveHeroIndex >= 0 && _currentActiveHeroIndex < _activeHeroes.Count)
            {
                return _activeHeroes[_currentActiveHeroIndex];
            }
            return null;
        }
    }
}
