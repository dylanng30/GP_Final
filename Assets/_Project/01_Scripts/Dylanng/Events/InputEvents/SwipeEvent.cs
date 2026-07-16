using Dylanng.Dylanng_EventBus;
using UnityEngine;

namespace Dylanng.Events.InputEvents
{
    public struct SwipeEvent : IEvent
    {
        public Vector2 Direction;
    }
}