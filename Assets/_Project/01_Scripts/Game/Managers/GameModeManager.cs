using Dylanng.Core;
using Dylanng.Core.Base;
using Dylanng.Core.State;
using Game.Events;
using Game.Gameplay.GameModeStates;

namespace Game.Managers
{
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

        private void OnEnable()
        {
            EventBus.Subscribe<SwitchGameModeEvent>(OnSwitchGameMode);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SwitchGameModeEvent>(OnSwitchGameMode);
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        public void ToggleMode()
        {
            if (_currentGPState == _exploreState) _currentGPState = _combatState;
            else _currentGPState = _exploreState;
            
            _stateMachine.ChangeState(_currentGPState);
        }

        private void OnSwitchGameMode(SwitchGameModeEvent evt)
        {
            if (evt.Mode == "Explore")
            {
                _currentGPState = _exploreState;
                _stateMachine.ChangeState(_exploreState);
            }
            else if (evt.Mode == "Combat")
            {
                _currentGPState = _combatState;
                _stateMachine.ChangeState(_combatState);
            }
        }
    }
}
