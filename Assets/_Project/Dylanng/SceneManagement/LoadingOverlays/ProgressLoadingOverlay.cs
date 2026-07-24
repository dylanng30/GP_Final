using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TCO_CustomSceneController
{
    public class ProgressLoadingOverlay : BaseLoadingOverlay
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private float fadeTime = 0.5f;

        public override IEnumerator StartLoad()
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
            }
            yield return FadeTo(1f, fadeTime);
        }

        public override IEnumerator EndLoad()
        {
            yield return FadeTo(0f, fadeTime);
        }
        
        public void UpdateProgress(float progress)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(progress);
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}