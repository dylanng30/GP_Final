using Dylanng.Dylanng_EventBus;

namespace Game.Events
{
    public struct ToggleFollowModeEvent : IEvent
    {
        public bool IsFollowModeEnabled;

        public ToggleFollowModeEvent(bool isEnabled)
        {
            IsFollowModeEnabled = isEnabled;
        }
    }
}
