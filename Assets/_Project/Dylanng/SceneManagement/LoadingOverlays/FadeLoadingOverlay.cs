using System;
using System.Collections;
using UnityEngine;

namespace TCO_CustomSceneController
{
    public class FadeLoadingOverlay : BaseLoadingOverlay
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInTime = 1f;
        [SerializeField] private float fadeOutTime = 1f;
        
        public override IEnumerator StartLoad()
        {
            // Fade In
            yield return FadeTo(1f, fadeInTime);
        }

        public override IEnumerator EndLoad()
        {
            // Fade Out
            yield return FadeTo(0f, fadeOutTime);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }
    }
}