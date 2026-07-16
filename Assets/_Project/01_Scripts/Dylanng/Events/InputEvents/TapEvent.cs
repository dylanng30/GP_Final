using Dylanng.Dylanng_EventBus;
using UnityEngine;

namespace Dylanng.Events.InputEvents
{
    public struct TapEvent : IEvent
    {
        public Vector3 ScreenPosition;
    }
}