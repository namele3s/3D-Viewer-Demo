using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// UI 按钮交互效果
/// 提供悬停高亮、点击缩放等视觉反馈
/// 挂载到 Button 组件所在的 GameObject 上
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("悬停效果")]
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private Color hoverTint = new Color(0.9f, 0.95f, 1f, 1f);

    [Header("点击效果")]
    [SerializeField] private bool enableClickEffect = true;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float clickDuration = 0.1f;

    private Vector3 originalScale;
    private Graphic targetGraphic;
    private Color originalColor;
    private Coroutine currentAnimation;
    private bool isHovering = false;

    void Awake()
    {
        originalScale = transform.localScale;

        // 获取按钮的图形组件（Image 或 Text）
        targetGraphic = GetComponent<Graphic>();
        if (targetGraphic != null)
            originalColor = targetGraphic.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverEffect) return;

        isHovering = true;
        StopCurrentAnimation();
        currentAnimation = StartCoroutine(ScaleAnimation(hoverScale, hoverDuration));

        if (targetGraphic != null)
            targetGraphic.color = hoverTint;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHoverEffect) return;

        isHovering = false;
        StopCurrentAnimation();
        currentAnimation = StartCoroutine(ScaleAnimation(1f, hoverDuration));

        if (targetGraphic != null)
            targetGraphic.color = originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableClickEffect) return;

        StopCurrentAnimation();
        currentAnimation = StartCoroutine(ScaleAnimation(clickScale, clickDuration));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enableClickEffect) return;

        StopCurrentAnimation();
        float targetScale = isHovering ? hoverScale : 1f;
        currentAnimation = StartCoroutine(ScaleAnimation(targetScale, clickDuration));
    }

    private IEnumerator ScaleAnimation(float targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // 平滑插值

            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
    }

    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    void OnDisable()
    {
        // 重置状态
        transform.localScale = originalScale;
        if (targetGraphic != null)
            targetGraphic.color = originalColor;
        isHovering = false;
    }
}
