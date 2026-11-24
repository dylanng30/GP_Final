using System.Collections.Generic;
using Dylanng;
using Game.Combat.Abilities.Events;
using Game.Events;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Managers.Hero;
using UnityEngine;

namespace Game.Managers.Party
{
    public class PartyCombatManager : ManagerBase
    {
        private List<HeroInstance> _activeHeroes = new List<HeroInstance>();
        private int _currentActiveHeroIndex = 0;
        private bool _hasSpawnedHeroes = false;

        public bool IsFollowModeEnabled { get; private set; }
        public IReadOnlyList<HeroInstance> ActiveHeroes => _activeHeroes;

        public override void Initialize()
        {
            ServiceLocator.Register<PartyCombatManager>(this);

            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null)
            {
                partyManager.OnPartyChanged += OnPartyChanged;
                // Delay SyncHeroes until the first scene is loaded
            }

            EventBus.Subscribe<ToggleFollowModeEvent>(OnToggleFollowMode);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            EventBus.Subscribe<ActorDiedEvent>(OnActorDied);
        }

        protected override void OnDestroy()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null)
            {
                partyManager.OnPartyChanged -= OnPartyChanged;
            }

            EventBus.Unsubscribe<ToggleFollowModeEvent>(OnToggleFollowMode);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
            EventBus.Unsubscribe<ActorDiedEvent>(OnActorDied);

            ClearHeroes();

            base.OnDestroy();
            
            ServiceLocator.Unregister<PartyCombatManager>();
        }

        private void OnPartyChanged()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager == null || !_hasSpawnedHeroes) return;

            if (TryReplaceChangedHeroes(partyManager.HeroIds))
            {
                EventBus.Publish(new PartyHeroesSpawnedEvent());
                return;
            }

            SyncHeroes(partyManager.HeroIds);
            InitializeCombat();
        }

        /// <summary>
        /// Chỉ thay những slot đổi hero, hero mới đứng đúng chỗ hero cũ và giữ quyền điều khiển nếu slot đó đang được điều khiển.
        /// Trả về false khi đội đổi sĩ số hoặc có slot trống, lúc đó phải dựng lại cả đội.
        /// </summary>
        private bool TryReplaceChangedHeroes(string[] heroIds)
        {
            if (heroIds == null || heroIds.Length != _activeHeroes.Count) return false;

            for (int slot = 0; slot < heroIds.Length; slot++)
            {
                if (string.IsNullOrEmpty(heroIds[slot]) || _activeHeroes[slot] == null) return false;
            }

            for (int slot = 0; slot < heroIds.Length; slot++)
            {
                if (_activeHeroes[slot].HeroId != heroIds[slot]) ReplaceHeroAt(slot, heroIds[slot]);
            }
            return true;
        }

        private void ReplaceHeroAt(int slot, string heroId)
        {
            HeroInstance prefab = ServiceLocator.Get<HeroDataManager>().GetHeroModel(heroId);
            if (prefab == null) return;

            HeroInstance oldHero = _activeHeroes[slot];
            bool isControlled = slot == _currentActiveHeroIndex;
            if (isControlled) oldHero.SetAsActive(false);

            HeroInstance newHero = Instantiate(prefab, oldHero.transform.position, oldHero.transform.rotation);
            newHero.Setup(heroId);
            _activeHeroes[slot] = newHero;
            Destroy(oldHero.gameObject);

            if (!isControlled) return;

            newHero.SetAsActive(true);
            EventBus.Publish(new ActiveHeroChangedEvent(slot, heroId));
        }

        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            if (!_hasSpawnedHeroes)
            {
                _hasSpawnedHeroes = true;
                var partyManager = ServiceLocator.Get<PartyManager>();
                if (partyManager != null)
                {
                    SyncHeroes(partyManager.HeroIds);
                    InitializeCombat();
                }
            }
        }

        private void OnToggleFollowMode(ToggleFollowModeEvent evt)
        {
            IsFollowModeEnabled = evt.IsFollowModeEnabled;
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
            EventBus.Publish(new PartyHeroesSpawnedEvent());
        }

        public void SwitchActiveHero(int index)
        {
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

        /// <summary>Hero đang điều khiển ngã thì chuyển quyền sang hero còn sống đầu tiên.</summary>
        private void OnActorDied(ActorDiedEvent evt)
        {
            HeroInstance activeHero = GetActiveHero();
            if (activeHero == null || evt.Actor != activeHero.gameObject) return;

            int nextIndex = _activeHeroes.FindIndex(hero => hero != null && !hero.IsDead);
            if (nextIndex >= 0) SwitchActiveHero(nextIndex);
        }

        /// <summary>Gọi đồng đội về ngay cạnh hero đang điều khiển.</summary>
        public void RegroupFollowers()
        {
            HeroInstance leader = GetActiveHero();
            if (leader == null) return;

            foreach (HeroInstance hero in _activeHeroes)
            {
                if (hero == null || hero == leader || hero.IsDead) continue;

                var aiController = hero.GetComponent<HeroAIController>();
                aiController.RegroupTo(leader.transform.position);
                aiController.FollowActiveHero();
            }
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

        public void BroadcastTargetToParty(ITargetable target, HeroInstance sourceHero)
        {
            if (target == null || target.IsDead) return;

            for (int i = 0; i < _activeHeroes.Count; i++)
            {
                var hero = _activeHeroes[i];
                if (hero != null && hero != sourceHero)
                {
                    var aiController = hero.GetComponent<HeroAIController>();
                    if (aiController != null && aiController.enabled)
                    {
                        aiController.SetTargetEnemy(target, broadcast: false);
                    }
                }
            }
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Target != null && evt.Target.TryGetComponent<ITargetable>(out var targetable) && !targetable.IsDead)
            {
                var casterHero = evt.Caster != null ? evt.Caster.GetComponent<HeroInstance>() : null;
                if (casterHero != null && _activeHeroes.Contains(casterHero))
                {
                    BroadcastTargetToParty(targetable, casterHero);
                }
            }
        }
    }
}
