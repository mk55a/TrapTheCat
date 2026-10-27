using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrapTheCat.UI
{
    public sealed class LoadingRing : MonoBehaviour
    {
        private const float HoldAtFullSeconds = 0.15f;

        [SerializeField] private Image _fill;

        public void SetProgress(float value)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            _fill.fillAmount = Mathf.Clamp01(value);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public IEnumerator Sweep(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetProgress(t / seconds);
                yield return null;
            }
            yield return HoldFull();
        }

        public IEnumerator Finish()
        {
            const float seconds = 0.35f;
            float start = _fill.fillAmount;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetProgress(Mathf.Lerp(start, 1f, t / seconds));
                yield return null;
            }
            yield return HoldFull();
        }

        private IEnumerator HoldFull()
        {
            SetProgress(1f);
            yield return new WaitForSecondsRealtime(HoldAtFullSeconds);
        }
    }
}
