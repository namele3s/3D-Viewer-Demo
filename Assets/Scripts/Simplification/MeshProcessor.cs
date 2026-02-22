using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityMeshSimplifier;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 网格处理器。
/// 负责缓存原始网格数据，并根据用户设定的质量比例执行 QEM 简化。
/// 通过 EventBus 订阅模型加载/卸载事件，不直接引用 ModelLoader。
/// </summary>
public class MeshProcessor : MonoBehaviour
{
    [Header("设置")]
    public Transform modelContainer;

    [Header("UI 组件")]
    public Slider qualitySlider;
    public TextMeshProUGUI valueLabel;
    public TextMeshProUGUI statsText;
    public Button simplifyButton;

    private int originalTriangleCount = 0;
    private Dictionary<int, Mesh> originalMeshCache = new Dictionary<int, Mesh>();

    void Start()
    {
        qualitySlider.onValueChanged.AddListener(OnSliderValueChange);
        simplifyButton.onClick.AddListener(StartSimplification);
        UpdateStats(0, 0);
    }

    // ★ 通过 EventBus 订阅事件，替代被 GetComponent 直接调用
    void OnEnable()
    {
        EventBus.OnModelLoaded += OnNewModelLoaded;
        EventBus.OnModelUnloading += OnModelUnloading;
    }

    void OnDisable()
    {
        EventBus.OnModelLoaded -= OnNewModelLoaded;
        EventBus.OnModelUnloading -= OnModelUnloading;
    }

    /// <summary>
    /// 响应 EventBus.OnModelLoaded 事件
    /// </summary>
    private void OnNewModelLoaded(Transform container)
    {
        modelContainer = container;
        CalculateOriginalStats();
    }

    /// <summary>
    /// 响应 EventBus.OnModelUnloading 事件，清理缓存
    /// </summary>
    private void OnModelUnloading()
    {
        originalMeshCache.Clear();
        originalTriangleCount = 0;
        UpdateStats(0, 0);
    }

    void OnSliderValueChange(float value)
    {
        valueLabel.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void UpdateStats(int current, int original = -1)
    {
        if (original != -1) originalTriangleCount = original;
        statsText.text = $"原始面数: {originalTriangleCount}\n当前面数: {current}";
    }

    public void CalculateOriginalStats()
    {
        originalMeshCache.Clear();

        // ★ 只统计原始网格，排除 DecisionEngine 生成的 LOD 子容器
        MeshFilter[] filters = GetOriginalFilters();
        int count = 0;

        foreach (var f in filters)
        {
            if (f.sharedMesh != null)
            {
                count += f.sharedMesh.triangles.Length / 3;
                originalMeshCache[f.GetInstanceID()] = f.sharedMesh;
            }
        }
        originalTriangleCount = count;
        UpdateStats(count, count);
    }

    /// <summary>
    /// 获取原始网格的 MeshFilter，排除 LODGroupGenerator 创建的子物体。
    /// LOD 子容器的命名规则为 "LOD1"、"LOD2" 等。
    /// </summary>
    private MeshFilter[] GetOriginalFilters()
    {
        return modelContainer.GetComponentsInChildren<MeshFilter>()
            .Where(f => !IsLODChild(f.transform))
            .ToArray();
    }

    /// <summary>
    /// 判断一个 Transform 是否属于 LODGroupGenerator 生成的 LOD 子容器。
    /// 通过向上遍历父节点，检查是否有名称匹配 "LOD1"、"LOD2" 等的容器。
    /// </summary>
    private bool IsLODChild(Transform t)
    {
        Transform current = t;
        while (current != null && current != modelContainer)
        {
            // LODGroupGenerator 创建的子容器命名为 "LOD1"、"LOD2" 等
            if (current.name.StartsWith("LOD") && current.name.Length <= 4
                && char.IsDigit(current.name[current.name.Length - 1]))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    void StartSimplification()
    {
        StartCoroutine(ProcessMeshes());
    }

    IEnumerator ProcessMeshes()
    {
        simplifyButton.interactable = false;
        float quality = qualitySlider.value;

        MeshFilter[] filters = GetOriginalFilters();

        if (filters.Length == 0)
        {
            simplifyButton.interactable = true;
            yield break;
        }

        int totalTriangles = 0;

        foreach (var filter in filters)
        {
            int id = filter.GetInstanceID();

            if (!originalMeshCache.ContainsKey(id)) continue;

            Mesh sourceMesh = originalMeshCache[id];

            if (Mathf.Approximately(quality, 1.0f))
            {
                filter.sharedMesh = sourceMesh;
                totalTriangles += sourceMesh.triangles.Length / 3;
            }
            else
            {
                var meshSimplifier = new MeshSimplifier();
                meshSimplifier.Initialize(sourceMesh);
                meshSimplifier.SimplifyMesh(quality);
                var destMesh = meshSimplifier.ToMesh();

                filter.sharedMesh = destMesh;
                totalTriangles += destMesh.triangles.Length / 3;
            }

            yield return null;
        }

        UpdateStats(totalTriangles);

        // ★ 简化完成后通过 EventBus 广播结果
        EventBus.FireMeshSimplified(totalTriangles, originalTriangleCount);

        simplifyButton.interactable = true;
        Debug.Log("简化/还原完成！");
    }
}
