using Dylanng;
using Game.UIs.Screens.Explore;

namespace Game.Gameplay.GameModeStates
{
    public class ExploreState : GamePlayStateBase
    {
        public override void Enter()
        {
            base.Enter();
            
            // Open Main HUD
            var uiManager = ServiceLocator.Get<UIManager>();
            uiManager?.OpenScreen<Screen_ExploreHUD>();

            // TODO: Disable Weapons, Enable NPC interactions
        }

        public override void Exit()
        {
            base.Exit();

        }
    }
}
