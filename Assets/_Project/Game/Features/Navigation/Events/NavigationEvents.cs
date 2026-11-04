using Dylanng;

namespace Game.Navigation.Events
{
    public struct TeleportRequestEvent : IEvent
    {
        public string TargetScene { get; }
        public string TargetPortalId { get; }
        public TCO_CustomSceneController.SceneNavigationSO TargetSceneSO { get; }

        public TeleportRequestEvent(string targetScene, string targetPortalId, TCO_CustomSceneController.SceneNavigationSO targetSceneSO = null)
        {
            TargetScene = targetScene;
            TargetPortalId = targetPortalId;
            TargetSceneSO = targetSceneSO;
        }
    }
}
