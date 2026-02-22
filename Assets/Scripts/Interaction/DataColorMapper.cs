using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 数据驱动变色控制器。
/// 读取模型各部件的数值数据（顶点数、面积等），按数值大小映射为渐变色覆盖到材质上。
/// 支持一键切换"变色模式"与"原始材质"。
/// 挂载到 AppManager 上。
/// </summary>
public class DataColorMapper : MonoBehaviour
{
    [Header("颜色梯度")]
    [Tooltip("数值从低到高的颜色渐变（默认: 蓝→绿→黄→红）")]
    [SerializeField] private Gradient colorGradient;

    [Header("数据源")]
    [Tooltip("变色依据的数据类型")]
    [SerializeField] private ColorDataSource dataSource = ColorDataSource.VertexCount;

    [Header("UI 组件")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI toggleLabel;

    // 原始材质备份：Key = Renderer 的 InstanceID，Value = 原始材质数组
    private Dictionary<int, Material[]> originalMaterialsBackup = new Dictionary<int, Material[]>();
    // 变色材质缓存（避免重复创建）
    private List<Material> colorMaterials = new List<Material>();

    private bool isColorMode = false;
    private Transform modelContainer;

    void Awake()
    {
        // Unity 序列化的 Gradient 默认是"白→白"，需要检测并替换为蓝绿黄红
        if (colorGradient == null || IsDefaultWhiteGradient(colorGradient))
        {
            colorGradient = CreateDefaultGradient();
        }
    }

    /// <summary>
    /// 检测渐变色是否为 Unity 默认的"白色→白色"（即未被用户手动配置）。
    /// </summary>
    private bool IsDefaultWhiteGradient(Gradient g)
    {
        if (g.colorKeys.Length != 2) return false;
        return g.colorKeys[0].color == Color.white && g.colorKeys[1].color == Color.white;
    }

    void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleColorMode);

        UpdateUI();
    }

    void OnEnable()
    {
        EventBus.OnModelLoaded += OnModelLoaded;
        EventBus.OnModelUnloading += OnModelUnloading;
    }

    void OnDisable()
    {
        EventBus.OnModelLoaded -= OnModelLoaded;
        EventBus.OnModelUnloading -= OnModelUnloading;
    }

    private void OnModelLoaded(Transform container)
    {
        // 如果之前在变色模式，先恢复
        if (isColorMode) RestoreOriginalMaterials();

        modelContainer = container;
        isColorMode = false;
        originalMaterialsBackup.Clear();
        CleanupColorMaterials();
        UpdateUI();
    }

    private void OnModelUnloading()
    {
        if (isColorMode) RestoreOriginalMaterials();
        modelContainer = null;
        isColorMode = false;
        originalMaterialsBackup.Clear();
        CleanupColorMaterials();
        UpdateUI();
    }

    // 用于传递给图例的数据范围
    private float currentMinVal = 0f;
    private float currentMaxVal = 0f;
    private string currentDataTitle = "";

    /// <summary>
    /// 切换变色模式。
    /// </summary>
    public void ToggleColorMode()
    {
        if (modelContainer == null) return;

        if (!isColorMode)
            ApplyColorMapping();
        else
            RestoreOriginalMaterials();

        isColorMode = !isColorMode;
        
        EventBus.FireColorModeChanged(isColorMode, currentMinVal, currentMaxVal, currentDataTitle);
        UpdateUI();
    }

    /// <summary>
    /// 应用数据变色：遍历所有 Renderer，按数据值映射颜色。
    /// </summary>
    private void ApplyColorMapping()
    {
        Renderer[] renderers = modelContainer.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 1. 收集所有部件的数据值
        float[] values = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            values[i] = GetDataValue(renderers[i].gameObject);
        }

        // 2. 计算最小值和最大值（用于归一化）
        currentMinVal = float.MaxValue;
        currentMaxVal = float.MinValue;
        foreach (float v in values)
        {
            if (v < currentMinVal) currentMinVal = v;
            if (v > currentMaxVal) currentMaxVal = v;
        }

        // 避免除以零
        float range = currentMaxVal - currentMinVal;
        if (range < 0.001f) range = 1f;

        currentDataTitle = GetDataSourceName(dataSource);
        Debug.Log($"[DataColorMapper] 数据范围: {currentMinVal:F0} ~ {currentMaxVal:F0} ({dataSource})");

        // 3. 备份原始材质 + 应用颜色
        CleanupColorMaterials();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            int id = renderer.GetInstanceID();

            // 备份原始材质（只备份一次）
            if (!originalMaterialsBackup.ContainsKey(id))
            {
                originalMaterialsBackup[id] = renderer.sharedMaterials;
            }

            // 归一化到 0~1
            float normalized = (values[i] - currentMinVal) / range;

            // 从渐变色中采样
            Color color = colorGradient.Evaluate(normalized);

            // ★ 基于原始材质创建副本，再修改颜色（保留原始 Shader，避免变白）
            Material baseMat = renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials[0] != null
                ? renderer.sharedMaterials[0]
                : new Material(Shader.Find("Standard"));

            Material colorMat = new Material(baseMat);
            colorMat.name = $"DataColor_{normalized:F2}";

            // ★ 清除纹理贴图（否则贴图会覆盖住我们设置的颜色）
            if (colorMat.HasProperty("baseColorTexture"))
                colorMat.SetTexture("baseColorTexture", null);  // glTFast
            if (colorMat.HasProperty("_BaseMap"))
                colorMat.SetTexture("_BaseMap", null);          // URP Lit
            if (colorMat.HasProperty("_MainTex"))
                colorMat.SetTexture("_MainTex", null);          // Built-in Standard

            // ★ 设置颜色（兼容三种着色器的颜色属性名）
            if (colorMat.HasProperty("baseColorFactor"))
                colorMat.SetColor("baseColorFactor", color);    // glTFast ShaderGraph
            if (colorMat.HasProperty("_BaseColor"))
                colorMat.SetColor("_BaseColor", color);         // URP Lit
            if (colorMat.HasProperty("_Color"))
                colorMat.SetColor("_Color", color);             // Built-in Standard

            colorMaterials.Add(colorMat);

            // 应用（所有子材质槽都用同一个颜色材质）
            Material[] newMats = new Material[renderer.sharedMaterials.Length];
            for (int j = 0; j < newMats.Length; j++)
                newMats[j] = colorMat;

            renderer.sharedMaterials = newMats;
        }
    }

    /// <summary>
    /// 恢复原始材质。
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        if (modelContainer == null) return;

        Renderer[] renderers = modelContainer.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            int id = renderer.GetInstanceID();
            if (originalMaterialsBackup.TryGetValue(id, out Material[] originalMats))
            {
                renderer.sharedMaterials = originalMats;
            }
        }

        originalMaterialsBackup.Clear();
        CleanupColorMaterials();

        Debug.Log("[DataColorMapper] 已恢复原始材质");
    }

    /// <summary>
    /// 根据选择的数据源，获取该 GameObject 的数值。
    /// </summary>
    private float GetDataValue(GameObject obj)
    {
        switch (dataSource)
        {
            case ColorDataSource.VertexCount:
                MeshFilter mf = obj.GetComponent<MeshFilter>();
                return mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0;

            case ColorDataSource.TriangleCount:
                MeshFilter mf2 = obj.GetComponent<MeshFilter>();
                return mf2 != null && mf2.sharedMesh != null ? mf2.sharedMesh.triangles.Length / 3f : 0;

            case ColorDataSource.BoundsVolume:
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null)
                {
                    Vector3 s = r.bounds.size;
                    return s.x * s.y * s.z;
                }
                return 0;

            case ColorDataSource.MetadataArea:
                ModelMetadata meta = obj.GetComponent<ModelMetadata>();
                return meta != null ? meta.area : 0;

            default:
                return 0;
        }
    }

    private string GetDataSourceName(ColorDataSource source)
    {
        switch (source)
        {
            case ColorDataSource.VertexCount: return "顶点数";
            case ColorDataSource.TriangleCount: return "面片数";
            case ColorDataSource.BoundsVolume: return "体积";
            case ColorDataSource.MetadataArea: return "面积";
            default: return "未定义";
        }
    }

    private Gradient CreateDefaultGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.2f, 0.4f, 1f), 0f),    // 蓝 (低)
                new GradientColorKey(new Color(0.2f, 0.9f, 0.4f), 0.33f), // 绿
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.66f),   // 黄
                new GradientColorKey(new Color(1f, 0.2f, 0.2f), 1f)       // 红 (高)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }

    private void CleanupColorMaterials()
    {
        foreach (var mat in colorMaterials)
        {
            if (mat != null) Destroy(mat);
        }
        colorMaterials.Clear();
    }

    private void UpdateUI()
    {
        if (toggleLabel != null)
        {
            toggleLabel.text = isColorMode ? "恢复原色" : "数据变色";
        }

        if (toggleButton != null)
            toggleButton.interactable = modelContainer != null;
    }

    void OnDestroy()
    {
        CleanupColorMaterials();
    }
}

/// <summary>
/// 变色数据源类型。
/// </summary>
public enum ColorDataSource
{
    VertexCount,     // 顶点数
    TriangleCount,   // 三角面数
    BoundsVolume,    // 包围盒体积
    MetadataArea     // 自定义元数据中的面积字段
}
