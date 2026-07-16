using Dylanng.Dylanng_EventBus;

namespace Dylanng.Events.SystemEvents
{
    public struct SceneLoadedEvent : IEvent
    {
        public string SceneName;
    }
}