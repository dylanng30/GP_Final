using Dylanng.Core;
using Dylanng.Core.Managers;
using Game.Managers.Party;
using Game.UIs.Screens.CombatHUD;

namespace Game.Gameplay.GameModeStates
{
    public class CombatState : GamePlayStateBase
    {
        public override void Enter()
        {
            base.Enter();
            GameLogger.Log("Entered Combat State");
            
            // Open Combat HUD
            var uiManager = ServiceLocator.Get<UIManager>();
            uiManager?.OpenScreen<Screen_CombatHUD>();

            // Initialize Combat Manager (Spawns heroes, sets up AI, activates weapons)
            // var partyCombatManager = ServiceLocator.Get<PartyCombatManager>();
            // partyCombatManager?.InitializeCombat();
        }

        public override void Exit()
        {
            base.Exit();
            GameLogger.Log("Exited Combat State");

            // Clean up Combat Manager
            // var partyCombatManager = ServiceLocator.Get<PartyCombatManager>();
            // partyCombatManager?.CleanupCombat();
        }
    }
}
