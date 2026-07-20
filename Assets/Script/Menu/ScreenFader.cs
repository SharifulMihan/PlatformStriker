using System.Collections;
using UnityEngine;

namespace MinimalMenu
{
    /// <summary>
    /// Fades a full-screen flat-color overlay in/out for smooth scene transitions.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        public IEnumerator FadeOut(float duration)
        {
            _group.blocksRaycasts = true;
            yield return Fade(0f, 1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            yield return Fade(1f, 0f, duration);
            _group.blocksRaycasts = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            _group.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
