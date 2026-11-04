using System;

namespace Game.Navigation.Data
{
    [Serializable]
    public class IntraScenePath
    {
        public string portalId1;
        public string portalId2;
        public float distanceCost;
    }
}
