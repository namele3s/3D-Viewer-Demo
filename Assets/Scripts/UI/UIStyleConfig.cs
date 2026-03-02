using UnityEngine;

/// <summary>
/// UI 样式配置
/// 统一管理整个项目的 UI 颜色、字体大小等样式
/// 创建方式：Assets → Create → 3DModelDemo → UI Style Config
/// </summary>
[CreateAssetMenu(fileName = "UIStyleConfig", menuName = "3DModelDemo/UI Style Config")]
public class UIStyleConfig : ScriptableObject
{
    [Header("浅色主题配色")]
    [Tooltip("面板背景色")]
    public Color panelBackground = new Color(0.98f, 0.98f, 0.98f, 0.95f);

    [Tooltip("面板边框色")]
    public Color panelBorder = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Tooltip("主要文本颜色")]
    public Color textPrimary = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Tooltip("次要文本颜色")]
    public Color textSecondary = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("强调色（按钮、高亮）")]
    public Color accentColor = new Color(0.3f, 0.6f, 0.9f, 1f);

    [Tooltip("按钮背景色")]
    public Color buttonBackground = new Color(0.95f, 0.95f, 0.95f, 1f);

    [Tooltip("按钮悬停色")]
    public Color buttonHover = new Color(0.9f, 0.95f, 1f, 1f);

    [Tooltip("按钮按下色")]
    public Color buttonPressed = new Color(0.85f, 0.9f, 0.95f, 1f);

    [Header("圆角设置")]
    [Range(0f, 50f)]
    public float panelCornerRadius = 12f;

    [Range(0f, 30f)]
    public float buttonCornerRadius = 8f;

    [Header("阴影设置")]
    public bool enableShadow = true;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.1f);
    public Vector2 shadowOffset = new Vector2(0f, -2f);

    [Header("动画设置")]
    [Range(0.1f, 1f)]
    public float fadeInDuration = 0.3f;

    [Range(0.1f, 1f)]
    public float fadeOutDuration = 0.25f;

    [Header("字体大小")]
    public int titleFontSize = 20;
    public int bodyFontSize = 14;
    public int smallFontSize = 12;

    [Header("间距")]
    public float panelPadding = 16f;
    public float elementSpacing = 8f;
}
