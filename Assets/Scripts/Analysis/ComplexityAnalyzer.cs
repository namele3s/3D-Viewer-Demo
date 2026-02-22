using UnityEngine;

/// <summary>
/// 复杂度分析器。
/// 统计模型的总顶点数和面片数，判定复杂度等级（High / Low）。
/// 纯静态工具类，无需挂载到 GameObject。
/// </summary>
public static class ComplexityAnalyzer
{
    /// <summary>
    /// 顶点数阈值。超过此值判定为"高复杂度"。
    /// 默认 10000 顶点 ≈ 一个中等精度的单体模型。
    /// </summary>
    public static int VertexThreshold = 10000;

    /// <summary>
    /// 复杂度分析结果，包含详细统计数据。
    /// </summary>
    public struct AnalysisResult
    {
        public int totalVertices;
        public int totalTriangles;
        public int meshCount;
        public ComplexityLevel level;
    }

    /// <summary>
    /// 分析模型复杂度：遍历所有 MeshFilter，统计总顶点数和三角面数。
    /// </summary>
    public static ComplexityLevel Analyze(GameObject obj)
    {
        AnalysisResult result = AnalyzeDetailed(obj);
        return result.level;
    }

    /// <summary>
    /// 详细分析：返回完整统计数据。
    /// </summary>
    public static AnalysisResult AnalyzeDetailed(GameObject obj)
    {
        MeshFilter[] filters = obj.GetComponentsInChildren<MeshFilter>();

        int totalVerts = 0;
        int totalTris = 0;
        int meshCount = 0;

        foreach (var filter in filters)
        {
            if (filter.sharedMesh != null)
            {
                totalVerts += filter.sharedMesh.vertexCount;
                totalTris += filter.sharedMesh.triangles.Length / 3;
                meshCount++;
            }
        }

        ComplexityLevel level = totalVerts >= VertexThreshold
            ? ComplexityLevel.High
            : ComplexityLevel.Low;

        Debug.Log($"[ComplexityAnalyzer] {obj.name}: " +
                  $"网格数={meshCount}, 顶点={totalVerts}, 三角面={totalTris}, " +
                  $"阈值={VertexThreshold} → {level}");

        return new AnalysisResult
        {
            totalVertices = totalVerts,
            totalTriangles = totalTris,
            meshCount = meshCount,
            level = level
        };
    }
}
