using UnityEngine;

/// <summary>
/// 处理策略数据结构。
/// 由 DecisionEngine 输出，描述一个模型应该如何被轻量化处理。
/// </summary>
[System.Serializable]
public class ProcessingStrategy
{
    /// <summary>模型类型标签（Building / Tree / Terrain / Generic）</summary>
    public ModelTag tag = ModelTag.Generic;

    /// <summary>大小分类</summary>
    public SizeCategory sizeCategory = SizeCategory.Small;

    /// <summary>复杂度等级</summary>
    public ComplexityLevel complexityLevel = ComplexityLevel.Low;

    /// <summary>是否需要生成 LOD</summary>
    public bool generateLOD = false;

    /// <summary>LOD 层级数（含原始级）。例如3表示 LOD0 + LOD1 + LOD2</summary>
    public int lodLevels = 3;

    /// <summary>各 LOD 层级的简化比例。索引0 = LOD0（最高精度），最后一个 = 最低精度</summary>
    public float[] lodQualityLevels = { 1.0f, 0.5f, 0.2f };

    /// <summary>一次性简化目标比例（不生成 LOD 时使用）</summary>
    public float simplifyQuality = 0.5f;

    /// <summary>是否需要简化</summary>
    public bool needsSimplification = true;

    /// <summary>是否保留平面特征（建筑类模型使用）</summary>
    public bool preserveFlatSurfaces = false;

    /// <summary>
    /// 返回策略的人类可读摘要，便于调试。
    /// </summary>
    public override string ToString()
    {
        if (!needsSimplification)
            return $"[{tag}] {sizeCategory}/{complexityLevel} → 跳过（保留原始）";
        if (generateLOD)
            return $"[{tag}] {sizeCategory}/{complexityLevel} → LOD x{lodLevels}层 ({string.Join(", ", lodQualityLevels)})";
        return $"[{tag}] {sizeCategory}/{complexityLevel} → 一次性简化 {simplifyQuality:P0}";
    }
}

/// <summary>模型类型标签枚举</summary>
public enum ModelTag
{
    Generic,    // 无标签，使用通用策略
    Building,   // 建筑
    Tree,       // 植被
    Terrain,    // 地形
    Furniture,  // 家具/小构件
    Vehicle     // 交通工具
}

/// <summary>大小分类枚举</summary>
public enum SizeCategory
{
    Small,  // 小型构件（桌椅、设备等）
    Large   // 大型物体（建筑、地形等）
}

/// <summary>复杂度等级枚举</summary>
public enum ComplexityLevel
{
    Low,    // 低复杂度（顶点数 < 阈值）
    High    // 高复杂度（顶点数 ≥ 阈值）
}
