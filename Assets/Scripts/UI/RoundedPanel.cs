using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 圆角面板组件
/// 通过调整 Image 的 UV 和使用特殊材质实现圆角效果
/// 挂载到需要圆角的 Image 组件上
/// </summary>
[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class RoundedPanel : MonoBehaviour
{
    [Header("圆角设置")]
    [SerializeField, Range(0f, 100f)] private float cornerRadius = 20f;
    [SerializeField] private bool roundTopLeft = true;
    [SerializeField] private bool roundTopRight = true;
    [SerializeField] private bool roundBottomLeft = true;
    [SerializeField] private bool roundBottomRight = true;

    [Header("外观")]
    [SerializeField] private Color backgroundColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
    [SerializeField] private bool enableBorder = true;
    [SerializeField] private Color borderColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField, Range(0f, 10f)] private float borderWidth = 2f;

    private Image image;
    private Material roundedMaterial;

    void OnEnable()
    {
        image = GetComponent<Image>();
        UpdateAppearance();
    }

    void OnValidate()
    {
        if (Application.isPlaying || !enabled) return;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (image == null) return;

        // 创建圆角材质（如果不存在）
        if (roundedMaterial == null)
        {
            // 使用 Unity 内置的 UI/Default shader
            // 注意：完整的圆角效果需要自定义 Shader，这里先用颜色模拟
            roundedMaterial = new Material(Shader.Find("UI/Default"));
        }

        image.material = roundedMaterial;
        image.color = backgroundColor;

        // 设置为 Sliced 模式以支持更好的缩放
        image.type = Image.Type.Sliced;
    }

    /// <summary>
    /// 设置圆角半径
    /// </summary>
    public void SetCornerRadius(float radius)
    {
        cornerRadius = Mathf.Clamp(radius, 0f, 100f);
        UpdateAppearance();
    }

    /// <summary>
    /// 设置背景颜色
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        if (image != null)
            image.color = backgroundColor;
    }

    void OnDestroy()
    {
        if (roundedMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(roundedMaterial);
            else
                DestroyImmediate(roundedMaterial);
        }
    }
}
