using UnityEngine;
using System.Collections;

/// <summary>
/// UI 面板淡入淡出动画控制器
/// 挂载到需要动画效果的面板根物体上
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIAnimator : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool scaleAnimation = true;
    [SerializeField] private Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);

    private CanvasGroup canvasGroup;
    private Coroutine currentAnimation;
    private Vector3 originalScale;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        // 面板激活时自动播放淡入动画
        if (canvasGroup != null)
        {
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// 淡入动画
    /// </summary>
    public IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        if (scaleAnimation)
            transform.localScale = startScale;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);

            canvasGroup.alpha = t;

            if (scaleAnimation)
                transform.localScale = Vector3.Lerp(startScale, originalScale, t);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        transform.localScale = originalScale;
    }

    /// <summary>
    /// 淡出动画（带回调）
    /// </summary>
    public void FadeOut(System.Action onComplete = null)
    {
        StopCurrentAnimation();
        currentAnimation = StartCoroutine(FadeOutCoroutine(onComplete));
    }

    private IEnumerator FadeOutCoroutine(System.Action onComplete)
    {
        canvasGroup.interactable = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - fadeCurve.Evaluate(elapsed / fadeDuration);

            canvasGroup.alpha = t;

            if (scaleAnimation)
                transform.localScale = Vector3.Lerp(startScale, originalScale, t);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        onComplete?.Invoke();
        gameObject.SetActive(false);
    }

    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
}
