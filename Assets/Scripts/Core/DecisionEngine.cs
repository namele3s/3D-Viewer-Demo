using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 多策略自适应决策引擎。
/// 模型加载后通过 EventBus 接收通知，自动分析模型特征并输出 ProcessingStrategy，
/// 然后触发 LOD 生成或一次性简化。
/// 
/// 挂载到场景中任意 GameObject 上即可工作。
/// </summary>
public class DecisionEngine : MonoBehaviour
{
    [Header("策略预设（在 Inspector 中拖入）")]
    [Tooltip("为不同类型标签创建不同的 StrategyPreset 资源，拖入此数组")]
    [SerializeField] private StrategyPresets[] presets;

    [Header("默认阈值（无匹配预设时使用）")]
    [SerializeField] private float defaultVolumeThreshold = 50f;
    [SerializeField] private int defaultVertexThreshold = 10000;

    [Header("调试")]
    [SerializeField] private bool logDecisions = true;

    // 预设查找字典：按 ModelTag 索引
    private Dictionary<ModelTag, StrategyPresets> presetMap;

    void Awake()
    {
        // 构建预设查找表
        presetMap = new Dictionary<ModelTag, StrategyPresets>();
        if (presets != null)
        {
            foreach (var preset in presets)
            {
                if (preset != null && !presetMap.ContainsKey(preset.targetTag))
                {
                    presetMap[preset.targetTag] = preset;
                }
            }
        }
    }

    void OnEnable()
    {
        EventBus.OnModelLoaded += OnModelLoaded;
    }

    void OnDisable()
    {
        EventBus.OnModelLoaded -= OnModelLoaded;
    }

    /// <summary>
    /// 模型加载完成后的自动处理流程。
    /// </summary>
    private void OnModelLoaded(Transform container)
    {
        // 遍历容器下的所有直接子物体（每个子物体视为一个独立模型）
        // 对于 glTF 加载，通常只有一个根节点
        for (int i = 0; i < container.childCount; i++)
        {
            GameObject model = container.GetChild(i).gameObject;
            ProcessingStrategy strategy = Analyze(model);

            if (logDecisions)
            {
                Debug.Log($"[DecisionEngine] 决策结果: {strategy}");
            }

            // 执行决策
            Execute(model, strategy);
        }
    }

    /// <summary>
    /// 分析单个模型，生成处理策略。
    /// 可被外部脚本直接调用以获得策略而不执行。
    /// </summary>
    public ProcessingStrategy Analyze(GameObject model)
    {
        // 1. 解析标签
        ModelTag tag = TagParser.Parse(model);

        // 2. 查找对应预设
        StrategyPresets preset = FindPreset(tag);

        // 3. 应用预设的阈值
        BoundingBoxAnalyzer.VolumeThreshold = preset != null
            ? preset.volumeThreshold
            : defaultVolumeThreshold;

        ComplexityAnalyzer.VertexThreshold = preset != null
            ? preset.vertexThreshold
            : defaultVertexThreshold;

        // 4. 分析特征
        SizeCategory size = BoundingBoxAnalyzer.Analyze(model);
        ComplexityLevel complexity = ComplexityAnalyzer.Analyze(model);

        // 5. 生成策略
        if (preset != null)
        {
            return preset.BuildStrategy(size, complexity);
        }
        else
        {
            // 无匹配预设：使用默认通用策略
            return BuildDefaultStrategy(tag, size, complexity);
        }
    }

    /// <summary>
    /// 执行处理策略。
    /// </summary>
    private void Execute(GameObject model, ProcessingStrategy strategy)
    {
        if (!strategy.needsSimplification && !strategy.generateLOD)
        {
            Debug.Log($"[DecisionEngine] {model.name}: 跳过处理（低复杂度小型构件）");
            return;
        }

        if (strategy.generateLOD)
        {
            // 调用 LOD 生成器
            LODGroupGenerator.GenerateLOD(model, strategy);
        }
        else if (strategy.needsSimplification)
        {
            // 一次性简化（不生成 LOD，仅降低面数）
            LODGroupGenerator.SimplifyOnly(model, strategy.simplifyQuality);
        }
    }

    /// <summary>
    /// 查找与标签匹配的预设。先精确匹配，未找到则回退到 Generic。
    /// </summary>
    private StrategyPresets FindPreset(ModelTag tag)
    {
        if (presetMap.TryGetValue(tag, out var preset))
            return preset;

        // 回退到 Generic 预设
        if (tag != ModelTag.Generic && presetMap.TryGetValue(ModelTag.Generic, out var fallback))
            return fallback;

        return null; // 完全没有预设，使用硬编码默认值
    }

    /// <summary>
    /// 硬编码的默认策略（当没有任何 ScriptableObject 预设时）。
    /// </summary>
    private ProcessingStrategy BuildDefaultStrategy(ModelTag tag, SizeCategory size, ComplexityLevel complexity)
    {
        var strategy = new ProcessingStrategy
        {
            tag = tag,
            sizeCategory = size,
            complexityLevel = complexity
        };

        if (size == SizeCategory.Large)
        {
            strategy.generateLOD = true;
            strategy.lodLevels = 3;
            strategy.lodQualityLevels = new float[] { 1.0f, 0.5f, 0.2f };
            strategy.needsSimplification = (complexity == ComplexityLevel.High);
        }
        else
        {
            strategy.generateLOD = false;
            if (complexity == ComplexityLevel.High)
            {
                strategy.needsSimplification = true;
                strategy.simplifyQuality = 0.5f;
            }
            else
            {
                strategy.needsSimplification = false;
            }
        }

        return strategy;
    }
}
