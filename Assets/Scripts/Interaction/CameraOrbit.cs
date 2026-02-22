using UnityEngine;

/// <summary>
/// 轨道相机控制器。
/// 支持右键旋转、中键/Shift+左键平移、滚轮缩放。
/// 通过 EventBus 监听模型加载事件，自动将旋转中心重置到新模型。
/// </summary>
public class CameraOrbit : MonoBehaviour
{
    [Header("目标物体 (如果不填，默认以当前看向的点为中心)")]
    public Transform target;

    [Header("操作速度")]
    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;
    public float panSpeed = 10.0f;
    public float zoomRate = 30.0f;

    private float distance = 10.0f;
    private float x = 0.0f;
    private float y = 0.0f;

    private Vector3 targetPosition;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        if (target != null)
        {
            targetPosition = target.position;
            distance = Vector3.Distance(transform.position, target.position);
        }
        else
        {
            targetPosition = transform.position + transform.forward * 10f;
        }
    }

    // ★ 订阅模型加载事件，自动聚焦新模型
    void OnEnable()
    {
        EventBus.OnModelLoaded += OnNewModelLoaded;
    }

    void OnDisable()
    {
        EventBus.OnModelLoaded -= OnNewModelLoaded;
    }

    /// <summary>
    /// 模型加载后自动将旋转中心设置为新模型的 Bounds 中心，
    /// 并调整距离以适配模型大小。
    /// </summary>
    private void OnNewModelLoaded(Transform container)
    {
        Bounds bounds = CalculateBounds(container.gameObject);

        targetPosition = bounds.center;
        distance = bounds.size.magnitude * 1.5f;

        // 保持当前观察角度，只更新中心点和距离
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPosition;

        transform.rotation = rotation;
        transform.position = position;
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        return bounds;
    }

    void LateUpdate()
    {
        // 1. 【平移】 (中键 或 Shift+左键)
        if (Input.GetMouseButton(2) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0)))
        {
            float translateX = Input.GetAxis("Mouse X") * panSpeed * 0.02f * distance;
            float translateY = Input.GetAxis("Mouse Y") * panSpeed * 0.02f * distance;

            Vector3 move = transform.right * -translateX + transform.up * -translateY;
            targetPosition += move;
        }

        // 2. 【旋转】 (右键)
        if (Input.GetMouseButton(1))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            y = ClampAngle(y, -80, 80);
        }

        // 3. 【缩放】 (滚轮)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (System.Math.Abs(scroll) > 0.001f)
        {
            distance -= distance * scroll * (zoomRate * 0.02f);
            if (distance < 0.1f) distance = 0.1f;
        }

        // 4. 【应用变换】
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetPosition;

        transform.rotation = rotation;
        transform.position = position;
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
