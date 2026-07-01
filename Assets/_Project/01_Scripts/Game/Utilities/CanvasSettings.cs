using Dylanng.Core.Base;
using UnityEngine;

namespace GAME.Utilities
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasSettings : MonoBehaviourBase
    {
        [SerializeField] private Canvas canvas;

        protected override void Awake()
        {
            base.Awake();

            if (canvas == null) canvas = GetComponent<Canvas>();

            Setup();
        }

        private void Setup()
        {
            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceCamera:
                    canvas.worldCamera = Camera.main;
                    break;
                case RenderMode.ScreenSpaceOverlay:
                    break;
                case RenderMode.WorldSpace:
                    break;
            }
        }
    }
}