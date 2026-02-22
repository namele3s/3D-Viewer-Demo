using UnityEngine;
using UnityMeshSimplifier;

/// <summary>
/// LOD 自动生成器。
/// 根据 ProcessingStrategy 为模型生成多级 LOD Mesh，并自动配置 Unity LODGroup 组件。
/// 纯静态工具类，由 DecisionEngine 调用。
/// </summary>
public static class LODGroupGenerator
{
    /// <summary>
    /// 为模型生成完整的 LOD 层级。
    /// 在模型根物体上创建 LODGroup 组件，为每个 MeshFilter 生成多级简化 Mesh。
    /// </summary>
    public static void GenerateLOD(GameObject model, ProcessingStrategy strategy)
    {
        if (strategy.lodQualityLevels == null || strategy.lodQualityLevels.Length == 0)
        {
            Debug.LogWarning($"[LODGroupGenerator] {model.name}: lodQualityLevels 为空，跳过");
            return;
        }

        int levels = strategy.lodLevels;
        float[] qualities = strategy.lodQualityLevels;

        // 确保层级数和质量数组一致
        if (qualities.Length < levels)
        {
            Debug.LogWarning($"[LODGroupGenerator] qualityLevels 长度({qualities.Length})小于 lodLevels({levels})，使用实际长度");
            levels = qualities.Length;
        }

        // 获取所有原始 MeshFilter 和 Renderer
        MeshFilter[] originalFilters = model.GetComponentsInChildren<MeshFilter>();
        Renderer[] originalRenderers = model.GetComponentsInChildren<Renderer>();

        if (originalFilters.Length == 0)
        {
            Debug.LogWarning($"[LODGroupGenerator] {model.name}: 没有 MeshFilter，跳过");
            return;
        }

        // 如果已有 LODGroup，先移除
        LODGroup existingLOD = model.GetComponent<LODGroup>();
        if (existingLOD != null)
        {
            Object.DestroyImmediate(existingLOD);
        }

        // 创建 LODGroup
        LODGroup lodGroup = model.AddComponent<LODGroup>();
        LOD[] lods = new LOD[levels];

        // LOD0 = 原始模型（quality = 1.0），直接使用现有 Renderer
        lods[0] = new LOD(GetScreenTransition(0, levels), originalRenderers);

        Debug.Log($"[LODGroupGenerator] {model.name}: 生成 {levels} 级 LOD");

        // LOD1, LOD2, ... = 逐级简化的副本
        for (int lodLevel = 1; lodLevel < levels; lodLevel++)
        {
            float quality = qualities[lodLevel];

            // 为此 LOD 级别创建一个子容器
            GameObject lodContainer = new GameObject($"LOD{lodLevel}");
            lodContainer.transform.SetParent(model.transform, false);

            Renderer[] lodRenderers = new Renderer[originalFilters.Length];

            for (int m = 0; m < originalFilters.Length; m++)
            {
                MeshFilter originalFilter = originalFilters[m];
                if (originalFilter.sharedMesh == null) continue;

                // 创建简化副本
                GameObject lodMeshObj = new GameObject($"{originalFilter.name}_LOD{lodLevel}");
                lodMeshObj.transform.SetParent(lodContainer.transform, false);

                // 复制 Transform（相对位置）
                lodMeshObj.transform.localPosition = originalFilter.transform.localPosition;
                lodMeshObj.transform.localRotation = originalFilter.transform.localRotation;
                lodMeshObj.transform.localScale = originalFilter.transform.localScale;

                // 添加 MeshFilter + MeshRenderer
                MeshFilter newFilter = lodMeshObj.AddComponent<MeshFilter>();
                MeshRenderer newRenderer = lodMeshObj.AddComponent<MeshRenderer>();

                // 复制材质
                Renderer originalRenderer = originalFilter.GetComponent<Renderer>();
                if (originalRenderer != null)
                {
                    newRenderer.sharedMaterials = originalRenderer.sharedMaterials;
                }

                // QEM 简化
                Mesh simplifiedMesh = SimplifyMesh(originalFilter.sharedMesh, quality, strategy.preserveFlatSurfaces);
                newFilter.sharedMesh = simplifiedMesh;

                lodRenderers[m] = newRenderer;
            }

            float screenTransition = GetScreenTransition(lodLevel, levels);
            lods[lodLevel] = new LOD(screenTransition, lodRenderers);

            int triCount = CountTriangles(lodRenderers);
            Debug.Log($"  LOD{lodLevel}: quality={quality:F2}, 三角面={triCount}, 屏幕阈值={screenTransition:F2}");
        }

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();

        Debug.Log($"[LODGroupGenerator] {model.name}: LODGroup 配置完成 ✅");
    }

    /// <summary>
    /// 一次性简化模型（不生成 LOD），直接替换现有 Mesh。
    /// </summary>
    public static void SimplifyOnly(GameObject model, float quality)
    {
        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>();
        int totalOriginal = 0;
        int totalSimplified = 0;

        foreach (var filter in filters)
        {
            if (filter.sharedMesh == null) continue;

            totalOriginal += filter.sharedMesh.triangles.Length / 3;

            Mesh simplified = SimplifyMesh(filter.sharedMesh, quality, false);
            filter.sharedMesh = simplified;

            totalSimplified += simplified.triangles.Length / 3;
        }

        Debug.Log($"[LODGroupGenerator] {model.name}: 一次性简化完成 " +
                  $"(quality={quality:F2}, {totalOriginal} → {totalSimplified} 三角面, " +
                  $"减少 {(1 - (float)totalSimplified / Mathf.Max(totalOriginal, 1)):P1})");
    }

    /// <summary>
    /// 使用 UnityMeshSimplifier 执行 QEM 简化。
    /// </summary>
    private static Mesh SimplifyMesh(Mesh sourceMesh, float quality, bool preserveFlatSurfaces)
    {
        var simplifier = new MeshSimplifier();
        simplifier.Initialize(sourceMesh);

        // 如果是建筑类，启用保留平面特征的选项
        if (preserveFlatSurfaces)
        {
            var options = SimplificationOptions.Default;
            options.PreserveBorderEdges = true;
            options.PreserveSurfaceCurvature = true;
            simplifier.SimplificationOptions = options;
        }

        simplifier.SimplifyMesh(quality);
        return simplifier.ToMesh();
    }

    /// <summary>
    /// 计算 LOD 层级的屏幕占比过渡阈值。
    /// LOD0 = 0.5, LOD1 = 0.25, LOD2 = 0.1, 以此类推。
    /// </summary>
    private static float GetScreenTransition(int lodLevel, int totalLevels)
    {
        // 等比递减：LOD0 占屏幕 50%+ 时显示，最低级在 1/(2^n) 时切换
        if (lodLevel == 0) return 0.5f;
        if (lodLevel == totalLevels - 1) return 0.01f; // 最低级几乎总是显示

        // 中间级别线性插值
        float t = (float)lodLevel / (totalLevels - 1);
        return Mathf.Lerp(0.5f, 0.05f, t);
    }

    /// <summary>
    /// 统计 Renderer 数组的总三角面数。
    /// </summary>
    private static int CountTriangles(Renderer[] renderers)
    {
        int count = 0;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                count += mf.sharedMesh.triangles.Length / 3;
            }
        }
        return count;
    }
}
