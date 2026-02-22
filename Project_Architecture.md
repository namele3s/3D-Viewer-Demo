# Project Architecture — 单点真实数据源 (Single Source of Truth)

> **项目名称**：基于 Unity 平台的三维模型数字化处理与可视化技术研究
> **最后更新**：2026-02-22
> **维护者**：主程序 / 架构师

---

## 一、项目全局定义 (Project Definition)

### 1.1 核心目标

构建一条基于 Unity 引擎的**端到端三维模型数字化处理工具链**：从异构数据（BIM / 点云 / DEM）的统一导入，经过基于 QEM 的自适应轻量化处理与 LOD 生成，到富交互 UGUI 界面，最终一键发布为浏览器可直接运行的 WebGL 应用。

### 1.2 技术栈

| 层级 | 技术 / 工具 |
|---|---|
| 引擎 | Unity 6.3 LTS+, URP |
| 语言 | C# (.NET Standard 2.1) |
| 模型导入 | GLTFast (Runtime glTF/glb)、IfcOpenShell (BIM→FBX/glTF)、CloudCompare CLI (点云→Mesh) |
| 网格简化 | UnityMeshSimplifier (QEM 算法实现) |
| 压缩 | Unity 内置 Mesh Compression、Draco (可选) |
| UI | Unity UGUI + TextMeshPro |
| 发布 | Unity WebGL Build Pipeline、自定义 Editor 一键打包脚本 |
| 性能分析 | Unity Profiler、浏览器 DevTools |

### 1.3 最终交付物

1. **Unity 工具链项目**：可运行的完整 Unity 工程，包含所有模块的 Editor 工具与 Runtime 脚本。
2. **WebGL 演示包**：以数字校园为示范场景的浏览器端可访问应用。
3. **毕业论文**：配套的技术文档与实验数据分析。

---

## 二、模块化拆解 (Architecture Breakdown)

### 模块 A：异构数据导入与标准化预处理流水线

**职责**：将 BIM（.ifc）、点云（.las/.ply）、地形（DEM）等异构数据统一转换为 Unity 原生支持的格式，并将元数据绑定到 `GameObject`。

```
输入源                  外部工具 / 脚本                 Unity 内格式
──────────────────────────────────────────────────────────────────
.ifc (BIM)        →  IfcOpenShell 批处理脚本     →  .fbx / .gltf + 元数据 JSON
.las / .ply (点云) →  CloudCompare CLI (泊松重建)  →  .obj / .ply Mesh
DEM (灰度图)       →  C# TerrainData API          →  Unity Terrain (.raw 高度图)
```

**关键脚本**：
- `DataImporter.cs` — 统一入口，根据文件扩展名分发到对应的子处理器
- `BIMConverter.cs` — 调用 IfcOpenShell，解析并附加 BIM 元数据
- `PointCloudProcessor.cs` — 调用 CloudCompare CLI 执行泊松重建
- `TerrainGenerator.cs` — 读取 DEM 数据，通过 `TerrainData` API 生成 Unity 地形

---

### 模块 B：基于 QEM 的网格简化与底层压缩

**职责**：对标准化后的 Mesh 执行基于二次误差度量（QEM）的网格简化，并利用 Unity 内置压缩进一步减小体积。

**核心依赖**：`UnityMeshSimplifier`（已集成，对应项目中的 `Whinarn.UnityMeshSimplifier.Runtime.csproj`）

**处理流程**：
```
原始 Mesh
  │
  ├─ quality = 1.0 → 直接使用原始缓存 (跳过计算)
  │
  └─ quality < 1.0 → MeshSimplifier.Initialize(sourceMesh)
                       → MeshSimplifier.SimplifyMesh(quality)
                       → 输出简化后的 destMesh
                       → Unity Mesh Compression (High/Medium/Low)
```

**关键脚本**：
- `MeshProcessor.cs` — 已有，负责运行时简化与原始网格缓存（"保险箱"机制）
- `MeshCompressorEditor.cs`（新增）— Editor 脚本，在导入/构建时批量设置 Mesh Compression 级别

---

### 模块 C：多策略自适应决策引擎

**职责**：根据模型特征（大小、复杂度、类型标签）自动选择最优的轻量化策略，实现"零手动配置"的智能处理。

**二维分类矩阵**：

| | 低复杂度 (顶点 < 阈值) | 高复杂度 (顶点 ≥ 阈值) |
|---|---|---|
| **大型物体** (BBox 体积 > 阈值) | 仅 LOD 生成（无需简化） | LOD 生成 + 高强度 QEM 简化 |
| **小型构件** (BBox 体积 ≤ 阈值) | 跳过（保留原始） | 一次性 QEM 简化（无 LOD） |

**命名标签识别规则**：
- `Building_*.fbx` → 保留平面特征的保守简化策略
- `Tree_*.fbx` → 激进简化策略（更高压缩比）
- `Terrain_*` → 走 DEM/Terrain 专用流程
- 无标签 → 回退到基于大小+复杂度的通用策略

**关键脚本**：
- `DecisionEngine.cs` — 核心决策入口，读取模型特征 → 查矩阵 → 输出 `ProcessingStrategy`
- `BoundingBoxAnalyzer.cs` — 计算 Bounding Box 体积
- `ComplexityAnalyzer.cs` — 统计顶点/面数，判定复杂度等级
- `TagParser.cs` — 解析文件名中的类型标签
- `LODGroupGenerator.cs` — 基于距离的 LOD 自动切换脚本（生成 `LODGroup` 组件）
- `StrategyPresets.cs` — 各类型标签对应的简化参数预设（ScriptableObject）

---

### 模块 D：UGUI 交互系统与 WebGL 自动化构建打包

**职责**：构建丰富的用户交互界面，并实现一键 WebGL 打包发布。

**D.1 交互子系统**：
- **模型信息点击查询**：Raycast 点击 → 读取 GameObject 上附加的 BIM 元数据 → UI 面板弹出展示
- **预设路径漫游**：基于 `AnimationCurve` 或 `Cinemachine` 的相机路径动画
- **实时数据变色**：根据绑定数据（温度/楼层/类型等）动态修改材质颜色
- **简化质量调节**：已有，Slider 控制实时减面

**D.2 WebGL 发布子系统**：
- 自定义 `EditorWindow` 一键打包脚本
- 自动设置 WebGL Player Settings（压缩格式、内存大小、模板等）
- 构建后自动输出到指定目录

**关键脚本**：
- `ModelLoader_Improved.cs` — 已有，运行时 glTF 加载
- `CameraOrbit.cs` — 已有，轨道相机控制
- `ModelInfoPanel.cs`（新增）— 点击查询信息的 UI 面板
- `PathRoaming.cs`（新增）— 预设路径漫游控制器
- `DataColorMapper.cs`（新增）— 实时数据驱动变色
- `WebGLBuildTool.cs`（新增）— Editor 菜单下的一键打包工具

---

## 三、进度状态追踪器 (State Tracker)

### 模块 A：异构数据导入与标准化预处理流水线

#### A.1 BIM 数据导入
- [ ] 调研并选定 IfcOpenShell 版本，确认 Python/CLI 调用方式
- [ ] 编写 `BIMConverter.cs`：封装 IfcOpenShell 命令行调用，实现 .ifc → .fbx/.gltf 批量转换
- [ ] 编写 BIM 元数据解析逻辑：从 IFC 中提取属性 → 序列化为 JSON
- [ ] 编写元数据绑定脚本：将 JSON 元数据附加到对应 `GameObject` 的自定义 `MonoBehaviour` 上
- [ ] 编写单元测试：验证 .ifc 文件转换后在 Unity 中可正确加载并携带元数据

#### A.2 点云数据导入
- [ ] 安装并验证 CloudCompare CLI 在目标环境下的可用性
- [ ] 编写 `PointCloudProcessor.cs`：调用 CloudCompare CLI 执行泊松重建（.las/.ply → Mesh）
- [ ] 实现点云重建参数可配置化（重建深度、密度阈值等）
- [ ] 编写单元测试：验证点云转换后的 Mesh 质量与顶点数量

#### A.3 地形数据导入
- [ ] 编写 `TerrainGenerator.cs`：读取 DEM 灰度图 → 转换为 Unity .raw 高度图格式
- [ ] 通过 `TerrainData` API 自动创建 Unity Terrain 对象
- [ ] 实现地形纹理自动应用（根据高度/坡度分配纹理层）
- [ ] 编写单元测试：验证生成的 Terrain 高度数据与原始 DEM 一致

#### A.4 统一入口
- [ ] 编写 `DataImporter.cs`：根据文件扩展名（.ifc / .las / .ply / .tif 等）自动分发到对应子处理器
- [ ] 实现批量导入功能：支持拖拽文件夹批量处理
- [ ] 为 `DataImporter` 创建 Editor GUI（`EditorWindow`），提供可视化的导入操作界面

---

### 模块 B：基于 QEM 的网格简化与底层压缩

#### B.1 核心简化流程
- [x] 集成 UnityMeshSimplifier 库（已完成，`Whinarn.UnityMeshSimplifier.Runtime`）
- [x] 编写 `MeshProcessor.cs`：实现运行时 QEM 简化（已完成）
- [x] 实现"原始网格缓存"机制（"保险箱"字典），支持任意质量级别间无损切换（已完成）
- [ ] 优化简化计算性能：将 `SimplifyMesh()` 移至后台线程（C# Job System 或 `Task.Run`），避免主线程卡顿
- [ ] 添加简化进度回调：在 UI 上显示当前简化进度百分比

#### B.2 网格压缩
- [ ] 编写 `MeshCompressorEditor.cs`：在 Editor 模式下批量设置 `ModelImporter.meshCompression` 级别
- [ ] 对比测试不同压缩级别（Off / Low / Medium / High）对文件体积与视觉质量的影响
- [ ] 调研并评估 Draco 压缩作为补充方案的可行性

---

### 模块 C：多策略自适应决策引擎

#### C.1 模型特征分析
- [x] 编写 `BoundingBoxAnalyzer.cs`：自动计算模型 AABB 包围盒体积，输出大小分类（大型/小型）
- [x] 编写 `ComplexityAnalyzer.cs`：统计模型总顶点数与面片数，输出复杂度等级（低/高）
- [x] 编写 `TagParser.cs`：解析文件名前缀标签（`Building_`、`Tree_` 等），返回模型类型枚举

#### C.2 决策矩阵
- [ ] 定义 `ProcessingStrategy` 数据结构：包含简化比例、是否生成 LOD、LOD 层级数、压缩级别等参数
- [ ] 定义 `StrategyPresets.cs`（ScriptableObject）：为每种标签类型配置默认的处理参数预设
- [ ] 编写 `DecisionEngine.cs`：接收三个分析器的输出 → 查询二维分类矩阵 → 输出最终 `ProcessingStrategy`
- [ ] 编写单元测试：覆盖矩阵中所有组合（大型+高复杂+Building / 小型+低复杂+无标签 等）

#### C.3 LOD 自动生成
- [ ] 编写 `LODGroupGenerator.cs`：根据 `ProcessingStrategy` 中的层级数，调用 `MeshSimplifier` 生成多级 LOD Mesh
- [ ] 自动创建并配置 Unity `LODGroup` 组件，设置各级的屏幕占比阈值
- [ ] 实现基于距离的 LOD 平滑过渡（Cross-fade 或 Dithering）
- [ ] 编写性能测试：在包含 100+ 个不同复杂度模型的场景中验证 LOD 切换的流畅度与帧率

---

### 模块 D：UGUI 交互系统与 WebGL 自动化构建打包

#### D.1 模型信息点击查询
- [x] 编写 Raycast 点击检测脚本：鼠标/触屏点击 → 获取命中的 `GameObject`
- [x] 编写 `ModelInfoPanel.cs`：读取 `GameObject` 上的 BIM 元数据组件 → 在 UI 面板中展示
- [ ] 设计信息面板 UI 布局（支持展示名称、材质、楼层、面积等属性）

#### D.2 预设路径漫游
- [x] 编写 `PathRoaming.cs`：定义漫游路径点（`Transform[]` 或 `Cinemachine` 路径）
- [x] 实现"播放/暂停/速度调节"的漫游控制 UI
- [x] 实现平滑插值相机移动与朝向过渡

#### D.3 实时数据变色
- [x] 编写 `DataColorMapper.cs`：根据绑定数据源（JSON / CSV / 实时 API）动态修改材质颜色
- [x] 实现颜色映射规则配置（数值区间 → 颜色渐变）
- [x] 设计变色图例 UI（Legend），展示数值与颜色的对应关系

#### D.4 现有交互系统优化
- [x] 运行时 glTF/glb 模型加载（已完成，`ModelLoader_Improved.cs`）
- [x] 轨道相机控制（已完成，`CameraOrbit.cs`）
- [x] 简化质量 Slider 实时调节（已完成，`MeshProcessor.cs`）
- [x] 优化相机控制：修复偏心模型旋转抖动问题，改为以模型 Bounds 中心为旋转锚点（已通过 EventBus 重构完成）
- [x] 清理旧版代码：移除或归档 `ModelLoader.cs`（已完成）

#### D.5 WebGL 自动化发布
- [x] 编写 `WebGLBuildTool.cs`（Editor 脚本）：在 Unity 菜单栏中添加"一键发布 WebGL"入口
- [x] 自动配置 WebGL Player Settings（压缩方式 Brotli/Gzip、初始内存大小、线性颜色空间等）
- [x] 实现构建后自动输出到指定目录，并生成简易 HTML 宿主页面
- [ ] 测试 WebGL 包在 Chrome / Firefox / Edge 三个主流浏览器中的加载与渲染表现
- [ ] 性能验证：使用浏览器 DevTools 与 Unity Profiler 对比分析 WebGL 包的帧率、内存与加载时间

---

## 附录：文件结构约定

```
Assets/
├── Scripts/
│   ├── Core/                      # 核心基础设施
│   │   ├── DataImporter.cs        # 统一导入入口
│   │   └── DecisionEngine.cs      # 决策引擎
│   ├── Import/                    # 模块 A — 数据导入
│   │   ├── BIMConverter.cs
│   │   ├── PointCloudProcessor.cs
│   │   └── TerrainGenerator.cs
│   ├── Simplification/            # 模块 B — 网格简化
│   │   ├── MeshProcessor.cs       # (已有，迁移至此)
│   │   └── LODGroupGenerator.cs
│   ├── Analysis/                  # 模块 C — 特征分析
│   │   ├── BoundingBoxAnalyzer.cs
│   │   ├── ComplexityAnalyzer.cs
│   │   └── TagParser.cs
│   ├── Interaction/               # 模块 D — 交互
│   │   ├── ModelInfoPanel.cs
│   │   ├── PathRoaming.cs
│   │   ├── DataColorMapper.cs
│   │   ├── CameraOrbit.cs         # (已有，迁移至此)
│   │   └── ModelLoader_Improved.cs # (已有，迁移至此)
│   └── Config/                    # 配置与预设
│       └── StrategyPresets.cs     # ScriptableObject 简化策略预设
├── Editor/
│   ├── MeshCompressorEditor.cs
│   └── WebGLBuildTool.cs
├── StreamingAssets/                # 运行时加载的模型文件
├── Resources/                     # 预设资源
└── Scenes/
    └── DigitalCampus.unity        # 数字校园示范场景
```
