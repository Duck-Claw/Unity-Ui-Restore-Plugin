# Unity UI Restore Plugin MVP

This folder contains the first Unity Editor MVP for restoring UI from PSD Auto Slicer exports.

## Install

Copy this folder's `Assets/Editor/PSDUIRestore` directory into a Unity project.

The MVP expects the PSD Auto Slicer export folder to be inside the Unity project's `Assets` folder, for example:

```text
Assets/UIExports/Login/export_manifest.json
Assets/UIExports/Login/*.png
```

## Use

1. Open Unity.
2. Put the PSD Auto Slicer export folder somewhere under `Assets`.
3. Open `Tools > PSD Auto Slicer > Restore UI`.
4. Select the export folder that contains `export_manifest.json`, or drag the folder from Unity's Project panel into the `Export Folder` field.
5. Click `Build UGUI Prefab`.

Generated Prefabs are saved to:

```text
Assets/PSDUIRestore/Generated
```

## MVP Scope

- Compatible with PSD Auto Slicer `0.2.48` manifest shape.
- Matches PNGs by manifest file names.
- Matches renamed PNGs by `sha256` when the manifest provides it.
- Falls back to same-folder unique size matching when a renamed PNG has no usable hash.
- Builds a UGUI Canvas Prefab.
- Creates empty control nodes at the top of the generated root:
  `Bg`, `Top`, `TopLeft`, `TopRight`, `Left`, `Center`, `Right`, `Bottom`, `BottomLeft`, `BottomRight`.
- Control nodes are created with `anchoredPosition = 0,0` and `sizeDelta = 0,0`.
- Control node anchors:
  `Bg` and `Center` use stretch/stretch;
  `Top` uses top/stretch;
  `TopLeft` uses top/left;
  `TopRight` uses top/right;
  `Left` uses middle/left;
  `Right` uses middle/right;
  `Bottom` uses bottom/stretch;
  `BottomLeft` uses bottom/left;
  `BottomRight` uses bottom/right.
- Preserves manifest `layerPath` as grouping nodes.
- Converts PSD top-left coordinates to Unity centered UI coordinates.
- Configures PNG assets as Sprites.
- Applies Sprite borders for manifest nine-slice items.
- Reports missing PNGs.

## Known MVP Limits

- Export folders must be under the Unity project's `Assets` directory.
- Old manifests without `sha256` still depend on file-name matching.
- Prefab replacement is not implemented yet.
- Text layers are restored as Image nodes.
- Updating an existing generated Prefab is not implemented yet.
