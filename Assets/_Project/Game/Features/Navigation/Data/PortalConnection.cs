using System;

namespace GP_Final.Navigation.Data
{
    [Serializable]
    public class PortalConnection
    {
        public string fromSceneName;
        public string fromPortalId;
        public string toSceneName;
        public string toPortalId;
        public float transitionCost = 0f;
    }
}
