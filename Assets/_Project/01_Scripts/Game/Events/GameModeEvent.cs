using Dylanng.Core;

namespace Game.Events
{
    public struct SwitchGameModeEvent : IEvent
    {
        public string Mode;
        
        public SwitchGameModeEvent(string mode)
        {
            Mode = mode;
        }
    }
}
