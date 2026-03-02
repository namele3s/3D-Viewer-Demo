using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 变色图例 UI（Legend）。
/// 展示当前变色模式的颜色渐变条和最小/最大值标注。
/// 监听 EventBus.OnColorModeChanged 自动显示/隐藏。
/// 挂载到图例面板的根 GameObject 上（建议放在 Canvas 左下角）。
/// </summary>
public class ColorLegend : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private GameObject legendRoot;         // 图例容器（控制显隐）
    [SerializeField] private RawImage gradientBar;          // 渐变色条
    [SerializeField] private TextMeshProUGUI minValueText;  // 最小值标签
    [SerializeField] private TextMeshProUGUI maxValueText;  // 最大值标签
    [SerializeField] private TextMeshProUGUI titleText;     // 图例标题（"按顶点数着色"等）

    [Header("渐变设置")]
    [Tooltip("与 DataColorMapper 中使用相同的渐变色")]
    [SerializeField] private Gradient colorGradient;

    [Tooltip("渐变纹理宽度")]
    [SerializeField] private int textureWidth = 256;

    private UIAnimator animator;

    void Start()
    {
        if (legendRoot != null)
        {
            legendRoot.SetActive(false);
            animator = legendRoot.GetComponent<UIAnimator>();
        }

        GenerateGradientTexture();
    }

    void OnEnable()
    {
        EventBus.OnColorModeChanged += OnColorModeChanged;
    }

    void OnDisable()
    {
        EventBus.OnColorModeChanged -= OnColorModeChanged;
    }

    private void OnColorModeChanged(bool isColorMode, float min, float max, string title)
    {
        if (legendRoot == null) return;

        if (isColorMode)
        {
            UpdateLegend(min, max, title);
            legendRoot.SetActive(true);
        }
        else
        {
            // 使用淡出动画隐藏
            if (animator != null)
                animator.FadeOut();
            else
                legendRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 更新图例显示的数值范围和标题。
    /// 由 DataColorMapper 间接通过 EventBus 调用。
    /// </summary>
    private void UpdateLegend(float minValue, float maxValue, string dataName)
    {
        if (minValueText != null)
            minValueText.text = $"{minValue:F0}";

        if (maxValueText != null)
            maxValueText.text = $"{maxValue:F0}";

        if (titleText != null)
            titleText.text = $"按{dataName}着色";
    }

    /// <summary>
    /// 生成渐变色纹理并应用到 RawImage。
    /// </summary>
    private void GenerateGradientTexture()
    {
        if (gradientBar == null) return;

        // 如果没有设置渐变（Unity 默认白→白），使用蓝绿黄红
        if (colorGradient == null || 
            (colorGradient.colorKeys.Length == 2 
             && colorGradient.colorKeys[0].color == Color.white 
             && colorGradient.colorKeys[1].color == Color.white))
        {
            colorGradient = CreateDefaultGradient();
        }

        Texture2D tex = new Texture2D(textureWidth, 1);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < textureWidth; x++)
        {
            float t = (float)x / (textureWidth - 1);
            Color c = colorGradient.Evaluate(t);
            tex.SetPixel(x, 0, c);
        }

        tex.Apply();
        gradientBar.texture = tex;
    }

    private Gradient CreateDefaultGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.2f, 0.4f, 1f), 0f),
                new GradientColorKey(new Color(0.2f, 0.9f, 0.4f), 0.33f),
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.66f),
                new GradientColorKey(new Color(1f, 0.2f, 0.2f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }
}
