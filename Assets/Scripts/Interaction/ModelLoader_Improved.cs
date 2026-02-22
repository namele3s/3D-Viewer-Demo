using UnityEngine;
using UnityEngine.UI;
using GLTFast;
using System.IO;
using TMPro;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// 改进版模型加载器。
/// 负责从本地 StreamingAssets 或网络 URL 加载 glTF/glb 模型。
/// 加载完成后通过 EventBus 广播事件，不直接引用任何下游模块。
/// </summary>
public class ModelLoader_Improved : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button loadButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("容器")]
    [SerializeField] private Transform modelContainer;

    [Header("设置")]
    [SerializeField] private string[] supportedFormats = { ".glb", ".gltf" };

    private bool isLoading = false;
    private GameObject currentModel;
    private GltfImport currentGltfImport;

    void Start()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);

        UpdateStatus("准备就绪");
    }

    void OnDestroy()
    {
        CleanupCurrentModel();

        if (currentGltfImport != null)
        {
            currentGltfImport.Dispose();
            currentGltfImport = null;
        }
    }

    public async void OnLoadButtonClicked()
    {
        if (isLoading)
        {
            UpdateStatus("正在加载中，请稍候...", Color.yellow);
            return;
        }

        string fileName = inputField.text.Trim();
        if (!ValidateInput(fileName))
            return;

        await LoadModel(fileName);
    }

    private bool ValidateInput(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            UpdateStatus("请输入文件名或URL", Color.red);
            return false;
        }

        if (!fileName.StartsWith("http"))
        {
            bool hasValidExtension = false;
            foreach (var format in supportedFormats)
            {
                if (fileName.EndsWith(format, System.StringComparison.OrdinalIgnoreCase))
                {
                    hasValidExtension = true;
                    break;
                }
            }

            if (!hasValidExtension)
            {
                if (!fileName.Contains("."))
                {
                    inputField.text = fileName + ".glb";
                    UpdateStatus("自动添加了 .glb 扩展名", Color.yellow);
                }
                else
                {
                    UpdateStatus($"不支持的文件格式。支持的格式: {string.Join(", ", supportedFormats)}", Color.red);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task LoadModel(string fileName)
    {
        isLoading = true;
        SetLoadingUI(true);

        try
        {
            string filePath = BuildFilePath(fileName);
            UpdateStatus($"正在加载: {Path.GetFileName(filePath)}", Color.cyan);

            // ★ 通过 EventBus 通知所有模块：旧模型即将被清除
            EventBus.FireModelUnloading();
            CleanupCurrentModel();

            if (currentGltfImport != null)
            {
                currentGltfImport.Dispose();
            }

            currentGltfImport = new GltfImport();
            bool success = await currentGltfImport.Load(filePath);

            if (success)
            {
                await currentGltfImport.InstantiateMainSceneAsync(modelContainer);
                OnModelLoadSuccess();
            }
            else
            {
                OnModelLoadFailed(filePath);
            }
        }
        catch (System.Exception e)
        {
            UpdateStatus($"加载出错: {e.Message}", Color.red);
            Debug.LogError($"模型加载异常: {e}");
        }
        finally
        {
            isLoading = false;
            SetLoadingUI(false);
        }
    }

    private string BuildFilePath(string fileName)
    {
        if (fileName.StartsWith("http://") || fileName.StartsWith("https://"))
        {
            return fileName;
        }

        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        #if UNITY_WEBGL && !UNITY_EDITOR
            filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        #else
            if (!filePath.StartsWith("file://"))
            {
                filePath = "file://" + filePath;
            }
        #endif

        Debug.Log($"构建的文件路径: {filePath}");
        return filePath;
    }

    private void CleanupCurrentModel()
    {
        if (modelContainer.childCount > 0)
        {
            for (int i = modelContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(modelContainer.GetChild(i).gameObject);
            }
        }
    }

    private void OnModelLoadSuccess()
    {
        UpdateStatus("✅ 模型加载成功！", Color.green);

        if (modelContainer.childCount > 0)
        {
            currentModel = modelContainer.GetChild(0).gameObject;

            // ★ 为所有子网格自动添加 MeshCollider（Raycast 点击检测必需）
            MeshFilter[] filters = modelContainer.GetComponentsInChildren<MeshFilter>();
            foreach (var f in filters)
            {
                if (f.GetComponent<Collider>() == null && f.sharedMesh != null)
                {
                    MeshCollider col = f.gameObject.AddComponent<MeshCollider>();
                    col.sharedMesh = f.sharedMesh;
                }
            }

            // ★ 通过 EventBus 广播，CameraOrbit 会自动聚焦，MeshProcessor 会自动统计
            EventBus.FireModelLoaded(modelContainer);
        }
    }

    private void OnModelLoadFailed(string filePath)
    {
        string errorMsg = "❌ 加载失败！\n";

        if (!filePath.StartsWith("http"))
        {
            errorMsg += "请检查：\n";
            errorMsg += "1. 文件是否在 StreamingAssets 文件夹中\n";
            errorMsg += "2. 文件名是否正确（注意大小写）\n";
            errorMsg += "3. 文件格式是否为 .glb 或 .gltf";
        }
        else
        {
            errorMsg += "请检查：\n";
            errorMsg += "1. URL 是否正确\n";
            errorMsg += "2. 网络连接是否正常\n";
            errorMsg += "3. 服务器是否允许跨域访问";
        }

        UpdateStatus(errorMsg, Color.red);
        Debug.LogError($"模型加载失败: {filePath}");
    }



    private void SetLoadingUI(bool loading)
    {
        if (loadButton != null)
            loadButton.interactable = !loading;

        if (loadingIndicator != null)
            loadingIndicator.SetActive(loading);

        if (inputField != null)
            inputField.interactable = !loading;
    }

    private void UpdateStatus(string message, Color? color = null)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color ?? Color.white;
        }

        Debug.Log($"[ModelLoader] {message}");
    }

    public void LoadPresetModel(string modelPath)
    {
        inputField.text = modelPath;
        OnLoadButtonClicked();
    }

    public GameObject GetCurrentModel()
    {
        return currentModel;
    }


}
