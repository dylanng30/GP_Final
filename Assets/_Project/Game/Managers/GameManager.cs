using System.Collections;
using Dylanng;
using Game.Combat;
using Game.Combat.Domain;
using Game.Managers;
using Game.Managers.Hero;
using Game.Managers.Item;
using Game.Managers.Party;
using Game.Combat.Abilities.Managers;
using TCO_CustomSceneController;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Managers
{
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
        
        [Header("--- COMBAT ---")]
        [SerializeField] private CombatRulesSO combatRules;

        [Header("--- SCENE SETTINGS ---")]
        [SerializeField] private SceneNavigationSO bootstrapSceneNavigationSO;
        
#if UNITY_EDITOR
        /// <summary>Khóa SessionState để công cụ Editor chọn scene khởi động (ví dụ Combat Sandbox).</summary>
        public const string BootNavigationOverrideKey = "GameManager.BootNavigationOverride";
#endif

        private TickSystem _tickSystem;
        private PauseSystem _pauseSystem;
        private DataStorageService _storageService;
        private CombatResolver _combatResolver;
        private PartySynergyService _partySynergyService;

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

            if (combatRules == null)
            {
                GameLogger.LogError("[GameManager] Cannot boot game because CombatRules is not assigned!");
                yield break;
            }

            _combatResolver = new CombatResolver(combatRules);
            _combatResolver.Initialize();

            _partySynergyService = new PartySynergyService(combatRules);
            _partySynergyService.Initialize();

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
            
            // Tự động gán NavigationSceneAdapter vào GameManager (khỏi lo bị huỷ hoặc quên kéo)
            if (gameObject.GetComponent<Game.Navigation.Components.NavigationSceneAdapter>() == null)
            {
                gameObject.AddComponent<Game.Navigation.Components.NavigationSceneAdapter>();
            }

            gameObject.AddComponent<CursorCaptureController>();
            gameObject.AddComponent<SingleEventSystemGuard>();

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
            
            // Tạm thời load sang Scene A sau khi boot xong
            if (SceneController.Instance != null)
            {
                SceneController.SceneTransitionPlan plan;
                string loadedSceneName = "Scene_A";
                SceneNavigationSO bootNavigation = ResolveBootNavigation();

                if (bootNavigation != null)
                {
                    plan = SceneController.Instance.GetSceneTransitionPlan(bootNavigation);
                    loadedSceneName = bootNavigation.ActiveScene.SceneName;
                }
                else
                {
                    plan = new SceneController.SceneTransitionPlan()
                        .Load("Main", "Scene_A", setActive: true)
                        .WithOverlay()
                        .WithClearUnusedAssets();
                }
                
                yield return plan.Perform();
                EventBus.Publish(new SceneLoadedEvent { SceneName = loadedSceneName });

                // Hủy Scene Bootstrapper đi để giải phóng Camera và các Object thừa
                // Các Manager có DontDestroyOnLoad sẽ tự động tách ra nên không sợ mất
                var bootScene = SceneManager.GetSceneByName("Bootstrapper");
                if (bootScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(bootScene);
                }
            }
            else
            {
                GameLogger.LogError("[GameManager] SceneController is missing!");
            }
        }

        private SceneNavigationSO ResolveBootNavigation()
        {
#if UNITY_EDITOR
            string overridePath = UnityEditor.SessionState.GetString(BootNavigationOverrideKey, string.Empty);
            if (!string.IsNullOrEmpty(overridePath))
            {
                var overrideNavigation = UnityEditor.AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(overridePath);
                if (overrideNavigation != null) return overrideNavigation;
            }
#endif
            return bootstrapSceneNavigationSO;
        }

        private void OnDestroy()
        {
            // Only clear services if THIS instance is the active singleton.
            // (If this is a duplicate being destroyed, we shouldn't wipe the global services)
            if (Instance == this)
            {
                ServiceLocator.ClearAllServices();
            }
        }
    }
}

