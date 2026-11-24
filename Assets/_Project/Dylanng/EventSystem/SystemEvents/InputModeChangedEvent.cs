namespace Dylanng
{
    public struct InputModeChangedEvent : IEvent
    {
        public bool UsePCInput;

        public InputModeChangedEvent(bool usePCInput)
        {
            UsePCInput = usePCInput;
        }
    }
}
