using UnityEngine;

/// <summary>
/// 文件名标签解析器。
/// 从文件名或 GameObject 名称中提取类型标签前缀。
/// 约定：名称以 "Building_"、"Tree_" 等前缀开头。
/// 纯静态工具类，无需挂载到 GameObject。
/// </summary>
public static class TagParser
{
    /// <summary>
    /// 从名称中解析模型类型标签。
    /// 匹配规则（不区分大小写）：
    ///   - "Building_XXX" 或 "building_xxx" → ModelTag.Building
    ///   - "Tree_XXX" / "Plant_XXX" / "Vegetation_XXX" → ModelTag.Tree
    ///   - "Terrain_XXX" / "Ground_XXX" / "DEM_XXX" → ModelTag.Terrain
    ///   - "Furniture_XXX" / "Chair_XXX" / "Table_XXX" → ModelTag.Furniture
    ///   - "Vehicle_XXX" / "Car_XXX" → ModelTag.Vehicle
    ///   - 其他 → ModelTag.Generic
    /// </summary>
    public static ModelTag Parse(string name)
    {
        if (string.IsNullOrEmpty(name))
            return ModelTag.Generic;

        string lower = name.ToLowerInvariant();

        // 建筑类
        if (lower.StartsWith("building_") || lower.StartsWith("bldg_"))
            return ModelTag.Building;

        // 植被类
        if (lower.StartsWith("tree_") || lower.StartsWith("plant_") || lower.StartsWith("vegetation_"))
            return ModelTag.Tree;

        // 地形类
        if (lower.StartsWith("terrain_") || lower.StartsWith("ground_") || lower.StartsWith("dem_"))
            return ModelTag.Terrain;

        // 家具/小构件类
        if (lower.StartsWith("furniture_") || lower.StartsWith("chair_") || lower.StartsWith("table_") || lower.StartsWith("desk_"))
            return ModelTag.Furniture;

        // 交通工具类
        if (lower.StartsWith("vehicle_") || lower.StartsWith("car_") || lower.StartsWith("truck_"))
            return ModelTag.Vehicle;

        return ModelTag.Generic;
    }

    /// <summary>
    /// 从 GameObject 名称中解析标签。
    /// </summary>
    public static ModelTag Parse(GameObject obj)
    {
        ModelTag tag = Parse(obj.name);
        Debug.Log($"[TagParser] {obj.name} → {tag}");
        return tag;
    }

    /// <summary>
    /// 从文件路径中提取文件名（不含扩展名）再解析标签。
    /// </summary>
    public static ModelTag ParseFromPath(string filePath)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        ModelTag tag = Parse(fileName);
        Debug.Log($"[TagParser] 文件 \"{fileName}\" → {tag}");
        return tag;
    }
}
