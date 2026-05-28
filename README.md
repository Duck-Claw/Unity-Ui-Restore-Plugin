# Unity UI Restore Plugin

English | [中文](#中文)

Unity UI Restore Plugin is a Unity Editor tool that rebuilds UGUI layouts from PSD Auto Slicer exports. It reads PNG files and `export_manifest.json`, then generates a clean Canvas Prefab with restored hierarchy, positions, image nodes, and nine-slice settings.

## Features

- Read PSD Auto Slicer export folders containing `export_manifest.json`.
- Generate a UGUI Canvas Prefab under `Assets/PSDUIRestore/Generated`.
- Restore `RectTransform` position and size from PSD coordinates.
- Preserve PSD-like hierarchy from `layerPath`.
- Create Unity `Image` nodes from exported PNG assets.
- Configure PNG assets as Sprites.
- Apply Sprite borders and `Image.Type = Sliced` for nine-slice items.
- Match renamed PNGs with `sha256` when available, so assets can still be restored even after file-name changes.
- Report generated, missing, hash-matched, and fallback-matched assets.

## Install

Copy the plugin folder into a Unity project:

```text
UnityPackage/Assets/Editor/PSDUIRestore
```

Recommended target path:

```text
Assets/Editor/PSDUIRestore
```

After Unity recompiles, open:

```text
Tools > PSD Auto Slicer > Restore UI
```

## Usage

1. Export PNG files and `export_manifest.json` with PSD Auto Slicer.
2. Place the export folder under the Unity project's `Assets` folder, for example:

```text
Assets/UIExports/Login/export_manifest.json
Assets/UIExports/Login/*.png
```

3. Open `Tools > PSD Auto Slicer > Restore UI`.
4. Select or drag the export folder into the `Export Folder` field.
5. Click `Build UGUI Prefab`.
6. Check the generated Prefab in:

```text
Assets/PSDUIRestore/Generated
```

## Compatibility

Recommended upstream export:

- PSD Auto Slicer `0.2.49+`
- Manifest schema with `sha256`, `assetId`, `layerId`, `assetType`, and `zIndex`

Supported baseline:

- PSD Auto Slicer `0.2.48` manifest shape

Rename matching works best when the manifest includes `sha256`.

## Current MVP Limits

- Export folders must be under the Unity project's `Assets` folder.
- Prefab replacement is not implemented yet.
- Text layers are restored as Image nodes, not TextMeshPro.
- Updating an existing edited Prefab in place is not implemented yet.
- The current target UI stack is UGUI.

## Roadmap

- Mapping review UI for missing, duplicate, and low-confidence matches.
- Prefab Registry for replacing common UI components.
- Rule-based replacement by asset type, name, layer path, or `prefabHint`.
- Rebuild existing Prefabs while preserving project-side scripts where possible.
- Optional TextMeshPro conversion.
- Batch scanning and rebuild workflows.

---

# 中文

Unity UI Restore Plugin 是一个 Unity Editor 工具，用于读取 PSD Auto Slicer 导出的 PNG 和 `export_manifest.json`，自动在 Unity 中重建 UGUI Canvas Prefab。它可以恢复层级、位置、图片节点和九宫格设置，把“切图后手动拼回 Unity”的流程自动化。

## 核心功能

- 读取包含 `export_manifest.json` 的 PSD Auto Slicer 导出目录。
- 在 `Assets/PSDUIRestore/Generated` 下生成 UGUI Canvas Prefab。
- 根据 PSD 坐标还原 `RectTransform` 的位置和尺寸。
- 根据 `layerPath` 保留接近 PSD 的层级结构。
- 使用导出的 PNG 创建 Unity `Image` 节点。
- 自动把 PNG 配置为 Sprite。
- 对九宫格资源应用 Sprite Border，并设置 `Image.Type = Sliced`。
- 支持通过 `sha256` 识别被重命名的 PNG，即使文件名改了也能尽量正确拼回。
- 输出生成数量、缺失数量、hash 匹配数量和兜底匹配数量。

## 安装

把插件目录复制到 Unity 项目：

```text
UnityPackage/Assets/Editor/PSDUIRestore
```

推荐目标路径：

```text
Assets/Editor/PSDUIRestore
```

Unity 编译完成后，打开菜单：

```text
Tools > PSD Auto Slicer > Restore UI
```

## 使用

1. 先用 PSD Auto Slicer 导出 PNG 和 `export_manifest.json`。
2. 把导出目录放到 Unity 项目的 `Assets` 下，例如：

```text
Assets/UIExports/Login/export_manifest.json
Assets/UIExports/Login/*.png
```

3. 打开 `Tools > PSD Auto Slicer > Restore UI`。
4. 选择或拖入导出目录。
5. 点击 `Build UGUI Prefab`。
6. 到下面目录查看生成结果：

```text
Assets/PSDUIRestore/Generated
```

## 兼容性

推荐上游导出：

- PSD Auto Slicer `0.2.49+`
- manifest 包含 `sha256`、`assetId`、`layerId`、`assetType`、`zIndex`

兼容基线：

- PSD Auto Slicer `0.2.48` manifest 结构

PNG 重命名识别在 manifest 带 `sha256` 时效果最好。

## 当前 MVP 限制

- 导出目录必须在 Unity 项目的 `Assets` 目录下。
- 暂未实现 Prefab 替换。
- 文本图层当前按 Image 节点还原，不转 TextMeshPro。
- 暂未支持原地更新已经人工编辑过的 Prefab。
- 当前目标 UI 栈是 UGUI。

## 路线图

- 映射检查 UI：处理缺失、重复、低置信度匹配。
- Prefab Registry：替换项目内常用 UI 组件。
- 按资源类型、名称、路径或 `prefabHint` 做规则化替换。
- 重建已有 Prefab，并尽量保留项目侧脚本。
- 可选 TextMeshPro 转换。
- 批量扫描和批量重建流程。
