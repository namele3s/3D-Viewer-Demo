using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 预设路径漫游控制器（改进版）。
/// 根据模型的实际形状自适应生成椭圆路径，而非固定正圆。
/// 支持：播放 / 暂停 / 停止（退出）、ESC 退出、漫游中滚轮缩放。
/// 挂载到 AppManager 上。
/// </summary>
public class PathRoaming : MonoBehaviour
{
    [Header("漫游设置")]
    [Tooltip("漫游速度（完成一圈的秒数）")]
    [SerializeField] private float roamDuration = 25f;

    [Tooltip("路径距离倍数（1.2 = 刚好看清全貌）")]
    [SerializeField] private float distanceMultiplier = 1.2f;

    [Tooltip("相机高度倍数（相对于模型高度）")]
    [SerializeField] private float heightMultiplier = 0.5f;

    [Tooltip("自动生成的路径点数量")]
    [SerializeField] private int waypointCount = 12;

    [Tooltip("漫游中滚轮缩放灵敏度")]
    [SerializeField] private float scrollZoomSpeed = 0.15f;

    [Header("手动路径点（可选，不填则自动生成）")]
    [SerializeField] private Transform[] manualWaypoints;

    [Header("UI 组件")]
    [SerializeField] private Button playPauseButton;
    [SerializeField] private TextMeshProUGUI playPauseLabel;
    [SerializeField] private Button stopButton;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedLabel;

    // 运行时状态
    private List<Vector3> waypoints = new List<Vector3>();
    private Vector3 lookAtCenter;
    private float progress = 0f;
    private bool isRoaming = false;
    private bool isPaused = false;
    private float speedMultiplier = 1f;
    private float zoomOffset = 0f; // 滚轮缩放偏移量
    private Camera mainCamera;
    private CameraOrbit cameraOrbit;

    // 漫游前保存的相机状态
    private Vector3 savedCameraPos;
    private Quaternion savedCameraRot;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            cameraOrbit = mainCamera.GetComponent<CameraOrbit>();

        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(ToggleRoaming);

        if (stopButton != null)
            stopButton.onClick.AddListener(StopRoaming);

        if (speedSlider != null)
        {
            speedSlider.minValue = 0.25f;
            speedSlider.maxValue = 3f;
            speedSlider.value = 1f;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

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
        if (isRoaming) StopRoaming();

        if (manualWaypoints == null || manualWaypoints.Length == 0)
        {
            GenerateAdaptivePath(container.gameObject);
        }
        else
        {
            waypoints.Clear();
            foreach (var wp in manualWaypoints)
            {
                if (wp != null)
                    waypoints.Add(wp.position);
            }
            Bounds bounds = CalculateBounds(container.gameObject);
            lookAtCenter = bounds.center;
        }

        UpdateUI();
    }

    private void OnModelUnloading()
    {
        if (isRoaming) StopRoaming();
        waypoints.Clear();
        UpdateUI();
    }

    /// <summary>
    /// 根据模型实际形状生成自适应椭圆路径。
    /// X/Z 轴分别取模型在对应方向的尺寸，生成贴合轮廓的环绕路径。
    /// </summary>
    private void GenerateAdaptivePath(GameObject model)
    {
        Bounds bounds = CalculateBounds(model);
        lookAtCenter = bounds.center;

        // ★ 关键改进：按模型的 X 和 Z 方向分别计算半径（椭圆）
        float radiusX = bounds.extents.x * distanceMultiplier + bounds.extents.z * 0.3f;
        float radiusZ = bounds.extents.z * distanceMultiplier + bounds.extents.x * 0.3f;

        // 保证最小半径，避免太贴脸
        float minRadius = bounds.size.magnitude * 0.5f;
        radiusX = Mathf.Max(radiusX, minRadius);
        radiusZ = Mathf.Max(radiusZ, minRadius);

        float baseHeight = bounds.center.y + bounds.extents.y * heightMultiplier;

        waypoints.Clear();

        for (int i = 0; i < waypointCount; i++)
        {
            float angle = (float)i / waypointCount * Mathf.PI * 2f;

            // ★ 椭圆轨道
            float x = bounds.center.x + Mathf.Cos(angle) * radiusX;
            float z = bounds.center.z + Mathf.Sin(angle) * radiusZ;

            // 高度微浮动（增加动感，但幅度比之前更温和）
            float heightWave = Mathf.Sin(angle * 2f) * bounds.extents.y * 0.1f;
            float y = baseHeight + heightWave;

            waypoints.Add(new Vector3(x, y, z));
        }

        Debug.Log($"[PathRoaming] 生成椭圆路径: {waypointCount} 点, " +
                  $"半径X={radiusX:F1}, 半径Z={radiusZ:F1}, " +
                  $"模型尺寸={bounds.size}");
    }

    public void ToggleRoaming()
    {
        if (waypoints.Count < 2)
        {
            Debug.LogWarning("[PathRoaming] 路径点不足，请先加载模型");
            return;
        }

        if (!isRoaming)
            StartRoaming();
        else if (isPaused)
            ResumeRoaming();
        else
            PauseRoaming();
    }

    private void StartRoaming()
    {
        isRoaming = true;
        isPaused = false;
        progress = 0f;
        zoomOffset = 0f;

        if (mainCamera != null)
        {
            savedCameraPos = mainCamera.transform.position;
            savedCameraRot = mainCamera.transform.rotation;
        }

        if (cameraOrbit != null)
            cameraOrbit.enabled = false;

        Debug.Log("[PathRoaming] 开始漫游 ▶");
        UpdateUI();
    }

    private void PauseRoaming()
    {
        isPaused = true;
        Debug.Log("[PathRoaming] 暂停 ⏸");
        UpdateUI();
    }

    private void ResumeRoaming()
    {
        isPaused = false;
        Debug.Log("[PathRoaming] 继续 ▶");
        UpdateUI();
    }

    /// <summary>
    /// 停止漫游，恢复 CameraOrbit 手动控制。
    /// </summary>
    public void StopRoaming()
    {
        if (!isRoaming) return;

        isRoaming = false;
        isPaused = false;
        progress = 0f;
        zoomOffset = 0f;

        if (cameraOrbit != null)
            cameraOrbit.enabled = true;

        if (mainCamera != null)
        {
            mainCamera.transform.position = savedCameraPos;
            mainCamera.transform.rotation = savedCameraRot;
        }

        Debug.Log("[PathRoaming] 停止漫游，恢复手动控制 ⏹");
        UpdateUI();
    }

    void LateUpdate()
    {
        // ★ ESC 键随时退出漫游
        if (isRoaming && Input.GetKeyDown(KeyCode.Escape))
        {
            StopRoaming();
            return;
        }

        if (!isRoaming || isPaused || waypoints.Count < 2)
        {
            // 即使暂停中，也允许滚轮缩放
            if (isRoaming && isPaused)
                HandleScrollZoom();
            return;
        }

        // 漫游中也允许滚轮缩放
        HandleScrollZoom();

        // 推进进度
        float speed = (1f / roamDuration) * speedMultiplier;
        progress += Time.deltaTime * speed;

        if (progress >= 1f)
            progress -= 1f;

        // Catmull-Rom 样条插值
        Vector3 basePos = EvaluateCatmullRom(progress);

        // ★ 应用滚轮缩放偏移（沿路径点到中心的方向拉近/推远）
        Vector3 dirToCenter = (lookAtCenter - basePos).normalized;
        Vector3 currentPos = basePos + dirToCenter * zoomOffset;

        if (mainCamera != null)
        {
            mainCamera.transform.position = currentPos;

            Quaternion targetRot = Quaternion.LookRotation(lookAtCenter - currentPos);
            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 漫游过程中的滚轮缩放处理。
    /// </summary>
    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            zoomOffset += scroll * scrollZoomSpeed *
                (lookAtCenter - mainCamera.transform.position).magnitude;

            // 限制缩放范围，不让相机穿进模型里面
            float maxZoom = (waypoints[0] - lookAtCenter).magnitude * 0.8f;
            zoomOffset = Mathf.Clamp(zoomOffset, -maxZoom * 0.5f, maxZoom);
        }
    }

    private Vector3 EvaluateCatmullRom(float t)
    {
        int count = waypoints.Count;
        float f = t * count;
        int i = Mathf.FloorToInt(f);
        float localT = f - i;

        Vector3 p0 = waypoints[((i - 1) % count + count) % count];
        Vector3 p1 = waypoints[i % count];
        Vector3 p2 = waypoints[(i + 1) % count];
        Vector3 p3 = waypoints[(i + 2) % count];

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * localT +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * localT * localT +
            (-p0 + 3f * p1 - 3f * p2 + p3) * localT * localT * localT
        );
    }

    private void OnSpeedChanged(float value)
    {
        speedMultiplier = value;
        if (speedLabel != null)
            speedLabel.text = $"{value:F1}x";
    }

    private void UpdateUI()
    {
        if (playPauseLabel != null)
        {
            if (!isRoaming)
                playPauseLabel.text = "开始漫游";
            else if (isPaused)
                playPauseLabel.text = "继续";
            else
                playPauseLabel.text = "暂停";
        }

        if (playPauseButton != null)
            playPauseButton.interactable = waypoints.Count >= 2;

        // 停止按钮仅在漫游中可用
        if (stopButton != null)
            stopButton.interactable = isRoaming;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
}
