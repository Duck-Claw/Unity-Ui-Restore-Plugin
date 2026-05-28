# Unity UI Restore Plugin Product Shape

## Recommended Form

The MVP should be a Unity Editor plugin.

Reasons:

- The product output is native Unity content: Canvas, RectTransform, Image, Sprite import settings, and Prefab assets.
- The user needs to test the generated hierarchy directly in Unity.
- Sprite import settings and Prefab saving are Editor-only operations.
- Existing UI component replacement will depend on project Prefabs, project paths, and Unity-specific conventions.

## Near-Term Product Boundary

The first version is an importer inside Unity:

- Select a PSD Auto Slicer export folder.
- Read `export_manifest.json`.
- Match PNGs by the current manifest file names.
- Match renamed PNGs by `sha256` when the manifest provides it.
- Create a UGUI Canvas hierarchy.
- Create standard empty control nodes under the generated root for later professional layout work.
- Convert PSD top-left coordinates to Unity centered RectTransform coordinates.
- Configure PNGs as Sprites.
- Generate Image nodes.
- Save the result as a Prefab.
- Report generated and missing items.

## Later Add-On Shape

A standalone companion app may become useful later, but it should not be the MVP.

Good future uses for a standalone tool:

- Batch scan many export folders.
- Compute asset hashes before Unity import.
- Produce mapping review reports.
- Compare PSD Auto Slicer export versions.

The Unity plugin should remain the authoritative tool for building or updating Unity UI objects.

## Relationship With PSD Auto Slicer

Current MVP should stay compatible with PSD Auto Slicer `0.2.48`, and local PSD Auto Slicer `0.2.49` exports provide `manifestSchemaVersion: 2` plus hash identity fields.

Future PSD Auto Slicer output should add stronger identity fields:

- `assetId`
- `layerId`
- `assetType`
- `zIndex`
- `sha256`
- optional `visualHash`
- optional `prefabHint`
- root-level `sourceCanvasWidth` and `sourceCanvasHeight`

The Unity plugin should treat these as optional fields so it can import both current and future manifests.
