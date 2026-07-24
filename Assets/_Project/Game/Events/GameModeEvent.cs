using Dylanng;
using Game.Data.GameMode;

namespace Game.Events
{
    public struct SwitchGameModeEvent : IEvent
    {
        public GameModeType Mode;
        
        public SwitchGameModeEvent(GameModeType mode)
        {
            Mode = mode;
        }
    }
}
