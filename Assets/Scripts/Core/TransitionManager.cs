using System;
using System.Collections;
using UnityEngine;

public class TransitionManager : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.45f;
    [SerializeField] private float holdBlackTime = 0.08f;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    public IEnumerator PlayTransition(Action middleAction)
    {
        yield return FadeToBlack();

        middleAction?.Invoke();

        if (holdBlackTime > 0f)
            yield return new WaitForSeconds(holdBlackTime);

        yield return FadeFromBlack();
    }

    public IEnumerator FadeToBlack()
    {
        yield return Fade(0f, 1f, fadeOutDuration);
    }

    public IEnumerator FadeFromBlack()
    {
        yield return Fade(1f, 0f, fadeInDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float time = 0f;

        fadeCanvasGroup.alpha = from;
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = false;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = to > 0.95f;
        fadeCanvasGroup.interactable = false;
    }
}