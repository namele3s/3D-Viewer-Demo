#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// WebGL 一键发布工具（Editor 脚本）。
/// 在 Unity 菜单栏中添加"Tools → WebGL Build"入口，
/// 自动配置最优 PlayerSettings 后构建 WebGL 包。
/// </summary>
public class WebGLBuildTool
{
    private const string BuildFolder = "WebGL_Build";

    // ─────────────────────────────────────────────
    //  菜单入口
    // ─────────────────────────────────────────────

    [MenuItem("Tools/WebGL Build/一键发布 WebGL（推荐）")]
    public static void BuildWebGL()
    {
        // 让用户选择输出目录
        string defaultPath = GetDefaultBuildPath();
        string buildPath = EditorUtility.SaveFolderPanel(
            "选择 WebGL 构建输出目录", defaultPath, "");

        if (string.IsNullOrEmpty(buildPath))
            return; // 用户取消了

        if (!EditorUtility.DisplayDialog(
            "WebGL 构建确认",
            $"即将开始 WebGL 构建：\n\n" +
            $"• 输出目录: {buildPath}\n" +
            $"• 压缩方式: Gzip\n" +
            $"• 将自动配置最优 Player Settings\n\n" +
            $"构建过程可能需要几分钟，是否继续？",
            "开始构建", "取消"))
        {
            return;
        }

        ApplyOptimalSettings();
        ExecuteBuild(buildPath);
    }

    [MenuItem("Tools/WebGL Build/仅配置 Player Settings（不构建）")]
    public static void ApplySettingsOnly()
    {
        ApplyOptimalSettings();
        EditorUtility.DisplayDialog("配置完成", "WebGL Player Settings 已配置为最优预设。", "确定");
    }

    [MenuItem("Tools/WebGL Build/打开构建输出目录")]
    public static void OpenBuildFolder()
    {
        string path = GetDefaultBuildPath();
        if (Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            EditorUtility.DisplayDialog("提示", $"构建目录不存在：\n{path}\n\n请先执行一次构建。", "确定");
        }
    }

    // ─────────────────────────────────────────────
    //  自动配置 Player Settings
    // ─────────────────────────────────────────────

    private static void ApplyOptimalSettings()
    {
        Debug.Log("[WebGLBuildTool] 正在配置 WebGL Player Settings...");

        // ── 基本信息 ──
        PlayerSettings.companyName = "3DModelDemo";
        PlayerSettings.productName = "3D Model Viewer";

        // ── 颜色空间：线性（PBR 渲染必需） ──
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // ── WebGL 专用设置 ──
        // 压缩方式：Gzip（兼容性最好，Brotli 需要服务器额外配置）
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;

        // 解压缩回退：开启（确保在不支持 Gzip 的服务器上也能运行）
        PlayerSettings.WebGL.decompressionFallback = true;

        // 初始内存大小（MB）：对于 3D 模型查看器，256MB 是较安全的值
        PlayerSettings.WebGL.initialMemorySize = 256;

        // 数据缓存：开启（第二次加载时更快）
        PlayerSettings.WebGL.dataCaching = true;

        // WebGL 模板：使用 Default（最简洁）
        PlayerSettings.WebGL.template = "APPLICATION:Default";

        // ── 渲染设置 ──
        // 关闭自动图形 API，强制使用 WebGL 2.0
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

        // ── 优化设置 ──
        // Strip Engine Code：减小包体
        PlayerSettings.stripEngineCode = true;

        // IL2CPP 代码生成：优化包体大小
        PlayerSettings.SetIl2CppCodeGeneration(
            NamedBuildTarget.WebGL,
            Il2CppCodeGeneration.OptimizeSize);

        // 异常处理：仅保留显式 throw（减小包体，生产环境推荐）
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        Debug.Log("[WebGLBuildTool] ✅ Player Settings 配置完成");
        Debug.Log("  • 压缩: Gzip");
        Debug.Log("  • 内存: 256MB");
        Debug.Log("  • 颜色空间: Linear");
        Debug.Log("  • 图形 API: WebGL 2.0 (OpenGLES3)");
        Debug.Log("  • 代码优化: OptimizeSize");
    }

    // ─────────────────────────────────────────────
    //  执行构建
    // ─────────────────────────────────────────────

    private static void ExecuteBuild(string buildPath)
    {

        // 确保输出目录存在
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        Debug.Log($"[WebGLBuildTool] 开始构建 → {buildPath}");

        // 收集当前 Build Settings 中的场景
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("错误",
                "Build Settings 中没有启用的场景！\n\n" +
                "请先通过 File → Build Settings 添加你的场景。",
                "确定");
            return;
        }

        // 执行构建
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        // 检查结果
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            // ★ 构建成功后生成一键启动脚本
            GenerateLauncher(buildPath);

            float sizeMB = report.summary.totalSize / (1024f * 1024f);
            float timeMin = (float)report.summary.totalTime.TotalMinutes;

            string msg = $"✅ WebGL 构建成功！\n\n" +
                         $"• 输出目录: {buildPath}\n" +
                         $"• 包体大小: {sizeMB:F1} MB\n" +
                         $"• 构建耗时: {timeMin:F1} 分钟\n" +
                         $"• 已生成「双击启动.bat」\n\n" +
                         $"答辩时双击「双击启动.bat」即可在浏览器中打开。\n" +
                         $"是否打开输出目录？";

            Debug.Log($"[WebGLBuildTool] ✅ 构建成功！大小={sizeMB:F1}MB，耗时={timeMin:F1}分钟");

            if (EditorUtility.DisplayDialog("构建成功", msg, "打开目录", "关闭"))
            {
                EditorUtility.RevealInFinder(buildPath);
            }
        }
        else
        {
            string errorMsg = $"❌ 构建失败！\n\n" +
                              $"错误数: {report.summary.totalErrors}\n" +
                              $"警告数: {report.summary.totalWarnings}\n\n" +
                              $"请查看 Console 窗口获取详细错误信息。";

            Debug.LogError($"[WebGLBuildTool] ❌ 构建失败！错误数: {report.summary.totalErrors}");
            EditorUtility.DisplayDialog("构建失败", errorMsg, "确定");
        }
    }

    /// <summary>
    /// 构建成功后生成一键启动脚本。
    /// 双击即可启动本地服务器并在浏览器中打开。
    /// </summary>
    private static void GenerateLauncher(string buildPath)
    {
        // 生成 PowerShell 版启动器（Windows 自带，不依赖 Python）
        string batContent =
@"@echo off
chcp 65001 >nul
echo ============================================
echo   3D Model Viewer - WebGL 本地服务器
echo ============================================
echo.
echo 正在启动本地服务器...
echo 请勿关闭此窗口！关闭即停止服务器。
echo.

set PORT=8080

:: 尝试用 Python 启动（如果有的话更稳定）
where python >nul 2>&1
if %errorlevel%==0 (
    echo [使用 Python 服务器]
    start http://localhost:%PORT%
    python -m http.server %PORT%
) else (
    echo [使用 PowerShell 服务器]
    start http://localhost:%PORT%
    powershell -ExecutionPolicy Bypass -Command ""$listener = New-Object System.Net.HttpListener; $listener.Prefixes.Add('http://localhost:%PORT%/'); $listener.Start(); Write-Host '服务器已启动: http://localhost:%PORT%'; while ($listener.IsListening) { $ctx = $listener.GetContext(); $path = $ctx.Request.Url.LocalPath; if ($path -eq '/') { $path = '/index.html' }; $file = Join-Path '%CD%' $path.TrimStart('/'); if (Test-Path $file) { $bytes = [IO.File]::ReadAllBytes($file); $ext = [IO.Path]::GetExtension($file); $mime = switch ($ext) { '.html'{'text/html'} '.js'{'application/javascript'} '.wasm'{'application/wasm'} '.data'{'application/octet-stream'} '.unityweb'{'application/octet-stream'} '.json'{'application/json'} '.css'{'text/css'} '.png'{'image/png'} '.jpg'{'image/jpeg'} default{'application/octet-stream'} }; $ctx.Response.ContentType = $mime; $ctx.Response.ContentLength64 = $bytes.Length; $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length) } else { $ctx.Response.StatusCode = 404 }; $ctx.Response.Close() }""
)

pause";

        string batPath = Path.Combine(buildPath, "双击启动.bat");
        File.WriteAllText(batPath, batContent, System.Text.Encoding.GetEncoding("GBK"));

        Debug.Log($"[WebGLBuildTool] 已生成启动脚本: {batPath}");
    }

    // ─────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────

    private static string GetDefaultBuildPath()
    {
        // 输出到项目根目录的 WebGL_Build 文件夹
        return Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            BuildFolder);
    }
}
#endif
