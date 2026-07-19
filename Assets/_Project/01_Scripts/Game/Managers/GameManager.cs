using System.Collections;
using Dylanng.Core;
using Dylanng.Core.Data;
using Dylanng.Core.Managers;
using Dylanng.Core.Pooling;
using Dylanng.Core.Systems;
using Dylanng.Core.Systems.TickSystem;
using Dylanng.Dylanng_EventBus;
using Dylanng.Events.SystemEvents;
using Dylanng.Services;
using Game.Managers;
using Game.Managers.Hero;
using Game.Managers.Item;
using Game.Managers.Party;
using Dylanng.Gameplay.RPG.Abilities.Managers;
using UnityEngine;

namespace GAME
{
    /// <summary>
    /// GameManager is the central bootstrap and root manager of the entire game lifecycle.
    /// Inherits from PersistentSingleton to persist across scene loads (DontDestroyOnLoad).
    /// Responsible for initializing core systems and business managers in a correct dependency order.
    /// </summary>
    public class GameManager : PersistentSingleton<GameManager>
    {
        [Header("--- CORE UI ---")]
        [SerializeField] private UIManager uiManager;

        [Header("--- DATA MANAGERS ---")]
        [SerializeField] private HeroDataManager heroDataManager;
        [SerializeField] private ItemDataManager itemDataManager;
        [SerializeField] private StatDataManager statDataManager;
        
        [Header("--- CORE MANAGERS ---")]
        [SerializeField] private PoolManager poolManager;
        [SerializeField] private InputManager inputManager;
        
        [Header("--- GAMEPLAY MANAGERS ---")]
        [SerializeField] private PartyManager partyManager;
        [SerializeField] private PartyCombatManager partyCombatManager;
        [SerializeField] private GameModeManager gameModeManager;
        [SerializeField] private AbilityVFXManager abilityVFXManager;
        
        private TickSystem _tickSystem;
        private PauseSystem _pauseSystem;
        private DataStorageService _storageService;

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(InitializeFramework());
        }
        
        // Coroutine sequence initializing the game architecture based on dependencies:
        // 1. Core Systems (PauseSystem, TickSystem, StorageService)
        // 2. User Interface (UIManager)
        // 3. Static & Dynamic Data (HeroDataManager, ItemDataManager)
        // 4. Platform Managers (PoolManager, InputManager)
        // 5. Gameplay Managers (PartyManager, GameModeManager, PartyCombatManager)
        private IEnumerator InitializeFramework()
        {
            // 1. Initialize Core Systems
            _pauseSystem = new PauseSystem();
            _pauseSystem.Initialize();
            
            _tickSystem = new TickSystem();
            _tickSystem.Initialize();
            
            var tickDriver = gameObject.AddComponent<TickDriver>();
            tickDriver.Initialize(_tickSystem);
            
            _storageService = new DataStorageService();
            _storageService.Initialize();
            
            yield return null;

            // 2. Initialize UIManager
            uiManager.Initialize();
            
            yield return null;

            // 3. Initialize Hero, Item and Stat managers
            heroDataManager.Initialize();
            itemDataManager.Initialize();
            statDataManager.Initialize();

            yield return null;

            // 4. Initialize Pool and Input managers
            poolManager.Initialize();
            inputManager.Initialize();

            // Register InputManager into the TickSystem update loop
            yield return null;
            _tickSystem.Register(inputManager);

            yield return null;

            // 5. Initialize Party & GameMode managers
            partyManager.Initialize();

            yield return null;
            gameModeManager.Initialize();

            yield return null;
            partyCombatManager.Initialize();

            yield return null;
            abilityVFXManager.Initialize();

            yield return null;

            // Verify remote data is loaded successfully before booting
            if (!heroDataManager.IsRemoteDataLoaded)
            {
                GameLogger.LogError("[GameManager] Cannot boot game because HeroDataManager failed to load remote data!");
                yield break;
            }

            // Publish event indicating successful game boot
            EventBus.Publish(new GameBootedEvent());
        }

        private void OnDestroy()
        {
            // Clear all registered services when GameManager is destroyed
            ServiceLocator.ClearAllServices();
        }
    }
}

