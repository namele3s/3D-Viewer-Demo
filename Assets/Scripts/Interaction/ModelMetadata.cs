using UnityEngine;

/// <summary>
/// 通用模型元数据组件。
/// 附加到 GameObject 上，存储该物体的自定义属性信息。
/// 当前用于显示基本网格信息；未来模块 A 完成后，BIM 元数据也会写入此组件。
/// </summary>
public class ModelMetadata : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("构件名称（可手动填写或由导入脚本自动设置）")]
    public string partName = "";

    [Tooltip("构件类型/分类")]
    public string category = "";

    [Tooltip("所属楼层")]
    public string floor = "";

    [Header("自定义属性")]
    [Tooltip("材质描述")]
    public string materialInfo = "";

    [Tooltip("面积（平方米）")]
    public float area = 0f;

    [Tooltip("备注")]
    [TextArea(2, 5)]
    public string notes = "";

    /// <summary>
    /// 自动从 GameObject 和 Mesh 信息填充基本字段。
    /// 在没有 BIM 元数据的情况下，提供有意义的默认值。
    /// </summary>
    public void AutoFill()
    {
        if (string.IsNullOrEmpty(partName))
            partName = gameObject.name;

        // 尝试从 Renderer 获取材质信息
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && string.IsNullOrEmpty(materialInfo))
        {
            if (renderer.sharedMaterial != null)
                materialInfo = renderer.sharedMaterial.name;
        }
    }
}
