using Dylanng.Dylanng_EventBus;

namespace Dylanng.Events.SystemEvents
{
    public struct GamePausedEvent : IEvent
    {
        public bool IsPaused;
    }
}
