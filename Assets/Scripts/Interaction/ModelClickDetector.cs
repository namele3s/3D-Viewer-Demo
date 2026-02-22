using UnityEngine;

/// <summary>
/// 模型点击检测器。
/// 通过 Raycast 检测鼠标左键点击的模型部件，选中后高亮显示，
/// 并通过 EventBus 广播点击事件供 ModelInfoPanel 响应。
/// 挂载到场景中任意 GameObject（建议挂在 AppManager）。
/// </summary>
public class ModelClickDetector : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("用于发射射线的相机，不填则使用 Main Camera")]
    [SerializeField] private Camera raycastCamera;

    [Tooltip("高亮颜色")]
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.8f, 1f, 1f);

    [Tooltip("高亮边缘宽度（通过调整 Emission 实现）")]
    [SerializeField] private float highlightIntensity = 0.3f;

    // 当前选中的物体及其原始材质信息
    private GameObject selectedObject;
    private Material[] originalMaterials;
    private Color[] originalColors;

    void Update()
    {
        // 只响应鼠标左键点击（不是拖拽）
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            // 排除正在进行相机操作的情况（Shift+左键 = 平移）
            if (Input.GetKey(KeyCode.LeftShift)) return;

            HandleClick();
        }
    }

    private void HandleClick()
    {
        Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;

            // 如果点击了同一个物体，不重复处理
            if (hitObj == selectedObject) return;

            // 取消之前的高亮
            ClearHighlight();

            // 选中新物体
            selectedObject = hitObj;
            ApplyHighlight(hitObj);

            // 通过 EventBus 广播
            EventBus.FireModelPartClicked(hitObj);

            Debug.Log($"[ModelClickDetector] 选中: {hitObj.name}");
        }
        else
        {
            // 点击空白处 → 取消选中
            if (selectedObject != null)
            {
                ClearHighlight();
                selectedObject = null;
                EventBus.FireModelPartDeselected();
                Debug.Log("[ModelClickDetector] 取消选中");
            }
        }
    }

    /// <summary>
    /// 给选中物体添加高亮效果（通过临时修改材质颜色）。
    /// </summary>
    private void ApplyHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        // 备份原始材质和颜色
        originalMaterials = renderer.materials; // 注意：这里会创建材质实例
        originalColors = new Color[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            // 保存原始颜色
            if (originalMaterials[i].HasProperty("_Color"))
                originalColors[i] = originalMaterials[i].color;
            else
                originalColors[i] = Color.white;

            // 叠加高亮色
            originalMaterials[i].color = Color.Lerp(originalColors[i], highlightColor, highlightIntensity);

            // 如果材质支持 Emission，也开启一点发光效果
            if (originalMaterials[i].HasProperty("_EmissionColor"))
            {
                originalMaterials[i].EnableKeyword("_EMISSION");
                originalMaterials[i].SetColor("_EmissionColor", highlightColor * 0.2f);
            }
        }
    }

    /// <summary>
    /// 清除高亮，恢复原始材质颜色。
    /// </summary>
    private void ClearHighlight()
    {
        if (selectedObject == null) return;

        Renderer renderer = selectedObject.GetComponent<Renderer>();
        if (renderer == null || originalMaterials == null) return;

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            if (i < originalColors.Length)
            {
                originalMaterials[i].color = originalColors[i];

                if (originalMaterials[i].HasProperty("_EmissionColor"))
                {
                    originalMaterials[i].SetColor("_EmissionColor", Color.black);
                }
            }
        }

        originalMaterials = null;
        originalColors = null;
    }

    /// <summary>
    /// 检测鼠标是否在 UI 元素上方（避免穿透 UI 点击到模型）。
    /// </summary>
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    void OnDisable()
    {
        ClearHighlight();
    }
}
