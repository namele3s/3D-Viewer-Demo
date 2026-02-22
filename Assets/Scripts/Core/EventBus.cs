using UnityEngine;
using System;

/// <summary>
/// 轻量级全局事件总线。
/// 所有模块间通信统一通过此类的静态事件进行，实现完全解耦。
/// 任何脚本只需订阅/触发这里的事件，无需直接引用其他模块。
/// </summary>
public static class EventBus
{
    // ─────────────────────────────────────────────
    //  模型生命周期事件
    // ─────────────────────────────────────────────

    /// <summary>
    /// 模型加载成功后触发。
    /// 参数: modelContainer (Transform) — 模型所在的父容器
    /// 订阅者: MeshProcessor, DecisionEngine (未来), ModelInfoPanel (未来) 等
    /// </summary>
    public static event Action<Transform> OnModelLoaded;

    /// <summary>
    /// 旧模型即将被清除前触发。
    /// 用于让各模块清理自己的缓存（如 MeshProcessor 的原始网格缓存）。
    /// </summary>
    public static event Action OnModelUnloading;

    // ─────────────────────────────────────────────
    //  交互事件
    // ─────────────────────────────────────────────

    /// <summary>
    /// 用户点击了模型的某个子部件时触发。
    /// 参数: 被点击的 GameObject
    /// 订阅者: ModelInfoPanel
    /// </summary>
    public static event Action<GameObject> OnModelPartClicked;

    /// <summary>
    /// 用户取消选中时触发（点击空白处）。
    /// </summary>
    public static event Action OnModelPartDeselected;

    // ─────────────────────────────────────────────
    //  网格处理事件
    // ─────────────────────────────────────────────

    /// <summary>
    /// 网格简化完成后触发。
    /// 参数: (currentTriCount, originalTriCount)
    /// 订阅者: UI 状态显示等
    /// </summary>
    public static event Action<int, int> OnMeshSimplified;

    // ─────────────────────────────────────────────
    //  触发方法 (Fire Methods)
    //  统一通过这些方法触发，方便加日志 / 断点
    // ─────────────────────────────────────────────

    public static void FireModelLoaded(Transform container)
    {
        Debug.Log($"[EventBus] OnModelLoaded → container: {container.name}");
        OnModelLoaded?.Invoke(container);
    }

    public static void FireModelUnloading()
    {
        Debug.Log("[EventBus] OnModelUnloading");
        OnModelUnloading?.Invoke();
    }

    public static void FireMeshSimplified(int current, int original)
    {
        OnMeshSimplified?.Invoke(current, original);
    }

    public static void FireModelPartClicked(GameObject obj)
    {
        OnModelPartClicked?.Invoke(obj);
    }

    public static void FireModelPartDeselected()
    {
        OnModelPartDeselected?.Invoke();
    }

    // ─────────────────────────────────────────────
    //  数据可视化事件
    // ─────────────────────────────────────────────

    /// <summary>
    /// 变色模式切换时触发。
    /// 参数: 
    /// 1. isColorMode (true=变色模式开启, false=恢复原始材质)
    /// 2. minValue (当前数据的最小值)
    /// 3. maxValue (当前数据的最大值)
    /// 4. dataTitle (数据类型名称标题)
    /// </summary>
    public static event Action<bool, float, float, string> OnColorModeChanged;

    public static void FireColorModeChanged(bool isColorMode, float min = 0, float max = 0, string title = "")
    {
        Debug.Log($"[EventBus] OnColorModeChanged → {(isColorMode ? "变色模式" : "原始材质")}");
        OnColorModeChanged?.Invoke(isColorMode, min, max, title);
    }
}
