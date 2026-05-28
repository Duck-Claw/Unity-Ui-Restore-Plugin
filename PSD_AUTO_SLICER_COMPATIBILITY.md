# PSD Auto Slicer Compatibility Notes

Checked locally on 2026-05-26.

Current upstream development folder:

```text
/Users/bytedance/Documents/Codex 2026/PSD Auto Slicer/plugin-dev/uxp-plugin
```

Current compatible baseline:

```text
0.2.48
```

Current local development version after hash manifest changes:

```text
0.2.49
```

The current `export_manifest.json` writer is in:

```text
plugin-dev/uxp-plugin/src/main.js
```

Current item fields used by the Unity MVP:

- `name`
- `originalName`
- `layerPath`
- `variant`
- `file`
- `relativeFolder`
- `x`
- `y`
- `width`
- `height`
- `canvasSize.width`
- `canvasSize.height`
- `nineSlice.applied`
- `nineSlice.borders`
- `sha256`, when present, for renamed PNG matching

The MVP treats identity fields as optional:

- `id`
- `assetId`
- `layerId`
- `assetType`
- `zIndex`
- `sha256`
- `visualHash`
- `prefabHint`

## Upstream Changes Needed Later

For production-grade Unity reconstruction, PSD Auto Slicer should extend each manifest item with stable identity and matching metadata.

The local `plugin-dev/uxp-plugin/src/main.js` in version `0.2.49` has been updated to write:

- root `manifestSchemaVersion: 2`
- root `sourceCanvasWidth`
- root `sourceCanvasHeight`
- item `assetId`
- item `layerId`
- item `assetType`
- item `zIndex`
- item `sha256`

Recommended next upstream manifest additions:

```json
{
  "assetId": "stable-layer-or-export-id",
  "layerId": "photoshop-layer-id-when-available",
  "assetType": "Btn",
  "zIndex": 42,
  "sha256": "exact-content-hash",
  "visualHash": "perceptual-hash",
  "prefabHint": "ButtonConfirm"
}
```

Recommended root additions:

```json
{
  "sourceCanvasWidth": 1920,
  "sourceCanvasHeight": 1080,
  "manifestSchemaVersion": 2
}
```

The Unity plugin should continue to support `0.2.48` manifests while gradually using these new fields when present.
