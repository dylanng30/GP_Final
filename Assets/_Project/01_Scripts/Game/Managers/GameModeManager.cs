using Dylanng.Core;
using Dylanng.Core.Base;
using Dylanng.Core.State;
using Game.Data.GameMode;
using Game.Events;
using Game.Gameplay.GameModeStates;

namespace Game.Managers
{
    /// <summary>
    /// Manages the current game mode (Explore vs Combat) using a PushdownStateMachine.
    /// Listens to SwitchGameModeEvent and controls the lifecycle of gameplay states.
    /// </summary>
    public class GameModeManager : ManagerBase
    {
        private PushdownStateMachine _stateMachine;
        private ExploreState _exploreState;
        private CombatState _combatState;

        private GamePlayStateBase _currentGPState;

        public override void Initialize()
        {
            ServiceLocator.Register<GameModeManager>(this);

            _stateMachine = new PushdownStateMachine();
            _exploreState = new ExploreState();
            _combatState = new CombatState();

            // Default to Explore State
            _currentGPState = _exploreState;
            _stateMachine.ChangeState(_currentGPState);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<GameModeManager>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<SwitchGameModeEvent>(OnSwitchGameMode);
        }

        protected override void OnDisable()
        {
            EventBus.Unsubscribe<SwitchGameModeEvent>(OnSwitchGameMode);
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void OnSwitchGameMode(SwitchGameModeEvent evt)
        {
            switch (evt.Mode)
            {
                case GameModeType.Combat: 
                    _currentGPState = _combatState;
                    break;
                case GameModeType.Explore: 
                    _currentGPState = _exploreState;
                    break;
                default: 
                    break;
            }
            
            _stateMachine.ChangeState(_currentGPState);
        }

        #region --- CHEAT METHODS ---
        /// <summary>
        /// Toggles between ExploreState and CombatState for testing/cheat purposes.
        /// </summary>
        public void ToggleMode()
        {
            if (_currentGPState == _exploreState) 
                _currentGPState = _combatState;
            else 
                _currentGPState = _exploreState;
            
            _stateMachine.ChangeState(_currentGPState);
        }
        #endregion
    }
}
