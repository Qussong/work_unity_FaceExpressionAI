using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// CanvasGroup 확장 메서드 모음
/// - FadeIn/FadeOut : 알파값을 시간에 따라 보간하여 부드럽게 전환
/// - Activate/DeActivate : 알파값과 인터랙션을 즉시 전환
/// </summary>
public static class CanvasGroupExtensions
{
    /// <summary>지정한 시간 동안 알파를 1로 페이드인</summary>
    public static IEnumerator FadeIn(this CanvasGroup canvasGroup, float duration, Action onComplete = null)
    {
        return Fade(canvasGroup, 1f, duration, onComplete);
    }

    /// <summary>지정한 시간 동안 알파를 0으로 페이드아웃</summary>
    public static IEnumerator FadeOut(this CanvasGroup canvasGroup, float duration, Action onComplete = null)
    {
        return Fade(canvasGroup, 0f, duration, onComplete);
    }

    /// <summary>
    /// 지정한 시간 동안 알파를 목표값으로 선형 보간한다
    /// 완료 후 interactable과 blocksRaycasts를 알파 기준으로 설정한다
    /// </summary>
    public static IEnumerator Fade(this CanvasGroup canvasGroup, float targetAlpha, float duration, Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        // 최종값 보정
        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha > 0f;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;

        onComplete?.Invoke();
    }

    /// <summary>즉시 표시 - 알파 1, 인터랙션 활성화</summary>
    public static void Activate(this CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    /// <summary>즉시 숨김 - 알파 0, 인터랙션 비활성화</summary>
    public static void DeActivate(this CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
