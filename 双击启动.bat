@echo off
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
    powershell -ExecutionPolicy Bypass -Command "$listener = New-Object System.Net.HttpListener; $listener.Prefixes.Add('http://localhost:%PORT%/'); $listener.Start(); Write-Host '服务器已启动: http://localhost:%PORT%'; while ($listener.IsListening) { $ctx = $listener.GetContext(); $path = $ctx.Request.Url.LocalPath; if ($path -eq '/') { $path = '/index.html' }; $file = Join-Path '%CD%' $path.TrimStart('/'); if (Test-Path $file) { $bytes = [IO.File]::ReadAllBytes($file); $ext = [IO.Path]::GetExtension($file); $mime = switch ($ext) { '.html'{'text/html'} '.js'{'application/javascript'} '.wasm'{'application/wasm'} '.data'{'application/octet-stream'} '.unityweb'{'application/octet-stream'} '.json'{'application/json'} '.css'{'text/css'} '.png'{'image/png'} '.jpg'{'image/jpeg'} default{'application/octet-stream'} }; $ctx.Response.ContentType = $mime; $ctx.Response.ContentLength64 = $bytes.Length; $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length) } else { $ctx.Response.StatusCode = 404 }; $ctx.Response.Close() }"
)

pause