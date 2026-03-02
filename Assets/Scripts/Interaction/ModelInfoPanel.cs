using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 模型信息展示面板。
/// 监听 EventBus 的点击事件，读取被点击 GameObject 的信息并在 UI 面板中展示。
/// 支持显示：构件名称、网格统计、材质信息、包围盒尺寸、自定义元数据。
/// 挂载到信息面板的根 GameObject 上。
/// </summary>
public class ModelInfoPanel : MonoBehaviour
{
    [Header("面板 UI 组件")]
    [SerializeField] private GameObject panelRoot;        // 整个面板容器（控制显示/隐藏）
    [SerializeField] private TextMeshProUGUI titleText;   // 构件名称标题
    [SerializeField] private TextMeshProUGUI detailText;  // 详细信息文本
    [SerializeField] private Button closeButton;          // 关闭按钮

    private UIAnimator animator;

    void Start()
    {
        // 初始隐藏面板
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // 获取动画组件
        if (panelRoot != null)
            animator = panelRoot.GetComponent<UIAnimator>();

        // 绑定关闭按钮
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
    }

    void OnEnable()
    {
        EventBus.OnModelPartClicked += OnPartClicked;
        EventBus.OnModelPartDeselected += OnPartDeselected;
    }

    void OnDisable()
    {
        EventBus.OnModelPartClicked -= OnPartClicked;
        EventBus.OnModelPartDeselected -= OnPartDeselected;
    }

    /// <summary>
    /// 响应点击事件：收集信息并显示面板。
    /// </summary>
    private void OnPartClicked(GameObject obj)
    {
        string info = BuildInfoText(obj);

        if (titleText != null)
            titleText.text = obj.name;

        if (detailText != null)
            detailText.text = info;

        ShowPanel();
    }

    /// <summary>
    /// 响应取消选中事件。
    /// </summary>
    private void OnPartDeselected()
    {
        HidePanel();
    }

    /// <summary>
    /// 构建信息文本。优先使用 ModelMetadata 组件的数据，
    /// 如果没有则自动从 Mesh/Renderer 信息中提取。
    /// </summary>
    private string BuildInfoText(GameObject obj)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // ── 1. 检查是否有自定义元数据组件 ──
        ModelMetadata metadata = obj.GetComponent<ModelMetadata>();
        if (metadata != null)
        {
            if (!string.IsNullOrEmpty(metadata.category))
                sb.AppendLine($"<b>类型:</b>  {metadata.category}");
            if (!string.IsNullOrEmpty(metadata.floor))
                sb.AppendLine($"<b>楼层:</b>  {metadata.floor}");
            if (!string.IsNullOrEmpty(metadata.materialInfo))
                sb.AppendLine($"<b>材质:</b>  {metadata.materialInfo}");
            if (metadata.area > 0)
                sb.AppendLine($"<b>面积:</b>  {metadata.area:F2} m²");
            if (!string.IsNullOrEmpty(metadata.notes))
                sb.AppendLine($"<b>备注:</b>  {metadata.notes}");

            sb.AppendLine();
        }

        // ── 2. 网格统计信息（始终显示） ──
        sb.AppendLine("<b>━━ 网格信息 ━━</b>");

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Mesh mesh = meshFilter.sharedMesh;
            sb.AppendLine($"<b>顶点数:</b>  {mesh.vertexCount:N0}");
            sb.AppendLine($"<b>三角面:</b>  {mesh.triangles.Length / 3:N0}");
            sb.AppendLine($"<b>子网格:</b>  {mesh.subMeshCount}");
        }
        else
        {
            // 可能点击的是父物体，统计所有子网格
            MeshFilter[] childFilters = obj.GetComponentsInChildren<MeshFilter>();
            int totalVerts = 0, totalTris = 0, meshCount = 0;
            foreach (var f in childFilters)
            {
                if (f.sharedMesh != null)
                {
                    totalVerts += f.sharedMesh.vertexCount;
                    totalTris += f.sharedMesh.triangles.Length / 3;
                    meshCount++;
                }
            }
            sb.AppendLine($"<b>网格数:</b>  {meshCount}");
            sb.AppendLine($"<b>总顶点:</b>  {totalVerts:N0}");
            sb.AppendLine($"<b>总面数:</b>  {totalTris:N0}");
        }

        // ── 3. 材质信息 ──
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterials.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>━━ 材质信息 ━━</b>");
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null)
                    sb.AppendLine($"  • {mat.name}");
            }
        }

        // ── 4. 空间信息 ──
        sb.AppendLine();
        sb.AppendLine("<b>━━ 空间信息 ━━</b>");
        sb.AppendLine($"<b>位置:</b>  {obj.transform.position:F2}");
        sb.AppendLine($"<b>旋转:</b>  {obj.transform.eulerAngles:F1}");
        sb.AppendLine($"<b>缩放:</b>  {obj.transform.lossyScale:F3}");

        // 包围盒尺寸
        if (renderer != null)
        {
            Vector3 size = renderer.bounds.size;
            sb.AppendLine($"<b>尺寸:</b>  {size.x:F2} × {size.y:F2} × {size.z:F2} m");
        }

        return sb.ToString();
    }

    private void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void HidePanel()
    {
        if (panelRoot != null)
        {
            // 如果有动画组件，使用淡出动画
            if (animator != null)
                animator.FadeOut();
            else
                panelRoot.SetActive(false);
        }
    }
}
