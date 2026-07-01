using System.Collections;
using Dylanng.Core;
using Dylanng.Core.Managers;
using Dylanng.Core.Pooling;
using Dylanng.Core.Systems;
using Dylanng.Core.Systems.TickSystem;
using Dylanng.Events.SystemEvents;
using Dylanng.Services;
using Game.Data.Hero;
using Game.Managers;
using Game.Managers.Hero;
using Game.Managers.Item;
using Game.Managers.Party;
using Game.UIs.Screens.Hero;
using UnityEngine;

namespace GAME
{
    public class GameManager : PersistentSingleton<GameManager>
    {
        [SerializeField] private UIManager uiManager;

        [Header("---DATA---")]
        [SerializeField] private HeroDataManager heroDataManager;
        [SerializeField] private ItemDataManager itemDataManager;
        
        [Header("---OTHERS MANAGERS---")]
        
        [SerializeField] private PoolManager poolManager;
        [SerializeField] private InputManager inputManager;
        
        [Header("---GAMEPLAY MANAGERS---")]
        [SerializeField] private PartyManager partyManager;
        [SerializeField] private PartyCombatManager partyCombatManager;
        [SerializeField] private GameModeManager gameModeManager;
        
        public TickSystem _tickSystem;
        private PauseSystem _pauseSystem;

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(InitializeFramework());
        }

        private IEnumerator InitializeFramework()
        {
            // System
            _pauseSystem = new PauseSystem();
            _pauseSystem.Initialize();
            
            _tickSystem = new TickSystem();
            _tickSystem.Initialize();
            
            var tickDriver = gameObject.AddComponent<TickDriver>();
            tickDriver.Initialize(_tickSystem);
            
            yield return null;
            uiManager.Initialize();
            
            yield return null;
            // Load data
            heroDataManager.Initialize();
            itemDataManager.Initialize();

            yield return null;
            //Manager
            poolManager.Initialize();
            
            inputManager.Initialize();

            //Register ticker
            yield return null;
            _tickSystem.Register(inputManager);

            yield return null;
            partyManager.Initialize();

            yield return null;
            gameModeManager.Initialize();

            yield return null;
            partyCombatManager.Initialize();

            yield return null;
            if (!heroDataManager.IsRemoteDataLoaded)
            {
                GameLogger.LogError("[GameManager] Cannot boot game because HeroDataManager failed to load remote data!");
                yield break;
            }

            EventBus.Publish(new GameBootedEvent());
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PauseSystem>();
        }
        
        
    }
}
