using System;
using System.Collections;
using Dylanng.Core;
using Dylanng.Core.Pooling;
using Dylanng.Core.Systems;
using Dylanng.Core.Systems.TickSystem;
using Dylanng.Core.UI;
using Dylanng.Events.SystemEvents;
using Dylanng.Services;
using UnityEngine;

namespace Dylanng.Managers
{
    public class GameManager : PersistentSingleton<GameManager>
    {
        [Header("Managers")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private PoolManager _poolManager;
        [SerializeField] private InputManager inputManager;
        //[SerializeField] private SaveLoadManager _saveLoadManager;
        //[SerializeField] private LevelManager _levelManager;

        private TickSystem _tickSystem;
        private PauseSystem _pauseSystem;
        
        

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(InitializeFramework());
        }

        private IEnumerator InitializeFramework()
        {
            _pauseSystem = new PauseSystem();
            _pauseSystem.Initialize();
            
            _tickSystem = new TickSystem();
            _tickSystem.Initialize();
            
            var tickDriver = gameObject.AddComponent<TickDriver>();
            tickDriver.Initialize(_tickSystem);

            yield return null;

            //_saveLoadManager.Initialize();
            _poolManager.Initialize();
            uiManager.Initialize();
            inputManager.Initialize();
            //_levelManager.Initialize();

            //Register ticker
            yield return null;
            _tickSystem.Register(inputManager);


            yield return null;
            EventBus.Publish(new GameBootedEvent());
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PauseSystem>();
        }
        
        
    }
}
