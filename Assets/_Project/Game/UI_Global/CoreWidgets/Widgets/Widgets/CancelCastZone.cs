using Dylanng;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Widgets
{
    public class CancelCastZone : Singleton<CancelCastZone>
    {
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color activeColor = new Color(0.9f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private float hoverScale = 1.2f;

        private Vector3 _originalScale;
        public RectTransform RectTransform { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            RectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
            ResetState();
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void OnDisable()
        {
            ResetState();
        }

        public void SetHoverState(bool isHovered)
        {
            if (isHovered)
            {
                if (background != null) background.color = activeColor;
                transform.localScale = _originalScale * hoverScale;
            }
            else
            {
                if (background != null) background.color = normalColor;
                transform.localScale = _originalScale;
            }
        }

        public void ResetState()
        {
            if (background != null) background.color = normalColor;
            transform.localScale = _originalScale;
        }
    }
}
