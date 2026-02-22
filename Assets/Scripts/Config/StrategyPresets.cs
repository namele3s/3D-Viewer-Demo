using UnityEngine;

/// <summary>
/// 策略预设配置（ScriptableObject）。
/// 可在 Unity Inspector 中创建多个预设资源，为不同类型标签配置不同的处理参数。
/// 创建方式：Assets → Create → 3DModelDemo → Strategy Preset
/// </summary>
[CreateAssetMenu(fileName = "NewStrategyPreset", menuName = "3DModelDemo/Strategy Preset")]
public class StrategyPresets : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("此预设适用的模型类型标签")]
    public ModelTag targetTag = ModelTag.Generic;

    [Header("大小阈值")]
    [Tooltip("包围盒体积阈值（立方米），超过则判定为大型物体")]
    public float volumeThreshold = 50f;

    [Header("复杂度阈值")]
    [Tooltip("顶点数阈值，超过则判定为高复杂度")]
    public int vertexThreshold = 10000;

    [Header("LOD 设置")]
    [Tooltip("大型物体是否生成 LOD")]
    public bool enableLODForLargeObjects = true;

    [Tooltip("LOD 层级数（含原始级）")]
    [Range(2, 5)]
    public int lodLevels = 3;

    [Tooltip("各 LOD 层级的简化比例（从高到低）")]
    public float[] lodQualityLevels = { 1.0f, 0.5f, 0.2f };

    [Header("简化设置")]
    [Tooltip("高复杂度小型构件的一次性简化比例")]
    [Range(0.05f, 0.95f)]
    public float simplifyQuality = 0.5f;

    [Tooltip("是否保留平面特征（建筑类推荐开启）")]
    public bool preserveFlatSurfaces = false;

    /// <summary>
    /// 根据分析结果生成完整的 ProcessingStrategy。
    /// </summary>
    public ProcessingStrategy BuildStrategy(SizeCategory size, ComplexityLevel complexity)
    {
        var strategy = new ProcessingStrategy
        {
            tag = targetTag,
            sizeCategory = size,
            complexityLevel = complexity,
            preserveFlatSurfaces = preserveFlatSurfaces
        };

        // ──── 二维分类矩阵决策 ────
        if (size == SizeCategory.Large)
        {
            if (complexity == ComplexityLevel.High)
            {
                // 大型 + 高复杂度 → LOD 生成 + 高强度简化
                strategy.generateLOD = enableLODForLargeObjects;
                strategy.lodLevels = lodLevels;
                strategy.lodQualityLevels = lodQualityLevels;
                strategy.needsSimplification = true;
            }
            else
            {
                // 大型 + 低复杂度 → 仅 LOD 生成（无需额外简化）
                strategy.generateLOD = enableLODForLargeObjects;
                strategy.lodLevels = lodLevels;
                strategy.lodQualityLevels = lodQualityLevels;
                strategy.needsSimplification = false;
            }
        }
        else // Small
        {
            if (complexity == ComplexityLevel.High)
            {
                // 小型 + 高复杂度 → 一次性简化（无 LOD）
                strategy.generateLOD = false;
                strategy.needsSimplification = true;
                strategy.simplifyQuality = simplifyQuality;
            }
            else
            {
                // 小型 + 低复杂度 → 跳过（保留原始）
                strategy.generateLOD = false;
                strategy.needsSimplification = false;
            }
        }

        return strategy;
    }
}
