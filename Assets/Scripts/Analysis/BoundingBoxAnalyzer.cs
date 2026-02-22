using UnityEngine;

/// <summary>
/// 包围盒分析器。
/// 计算模型的 AABB 包围盒体积，判定大小分类（Large / Small）。
/// 纯静态工具类，无需挂载到 GameObject。
/// </summary>
public static class BoundingBoxAnalyzer
{
    /// <summary>
    /// 体积阈值（立方米）。超过此值判定为"大型物体"。
    /// 默认 50 立方米 ≈ 一个小房子（约 3.7m 边长的立方体）
    /// </summary>
    public static float VolumeThreshold = 50f;

    /// <summary>
    /// 计算目标 GameObject（含所有子物体）的 AABB 包围盒。
    /// </summary>
    public static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[BoundingBoxAnalyzer] {obj.name} 没有任何 Renderer，返回零尺寸 Bounds");
            return new Bounds(obj.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    /// <summary>
    /// 计算包围盒体积（立方米）。
    /// </summary>
    public static float CalculateVolume(Bounds bounds)
    {
        Vector3 size = bounds.size;
        return size.x * size.y * size.z;
    }

    /// <summary>
    /// 综合分析：计算体积并返回大小分类。
    /// </summary>
    public static SizeCategory Analyze(GameObject obj)
    {
        Bounds bounds = CalculateBounds(obj);
        float volume = CalculateVolume(bounds);

        SizeCategory result = volume > VolumeThreshold ? SizeCategory.Large : SizeCategory.Small;

        Debug.Log($"[BoundingBoxAnalyzer] {obj.name}: " +
                  $"尺寸={bounds.size}, 体积={volume:F2}m³, " +
                  $"阈值={VolumeThreshold}m³ → {result}");

        return result;
    }

    /// <summary>
    /// 综合分析（重载）：同时返回包围盒、体积和分类。
    /// </summary>
    public static SizeCategory Analyze(GameObject obj, out Bounds bounds, out float volume)
    {
        bounds = CalculateBounds(obj);
        volume = CalculateVolume(bounds);

        SizeCategory result = volume > VolumeThreshold ? SizeCategory.Large : SizeCategory.Small;

        Debug.Log($"[BoundingBoxAnalyzer] {obj.name}: " +
                  $"尺寸={bounds.size}, 体积={volume:F2}m³, " +
                  $"阈值={VolumeThreshold}m³ → {result}");

        return result;
    }
}
