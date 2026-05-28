using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PSDUIRestore
{
    [Serializable]
    public class PSDManifest
    {
        public string tool;
        public string version;
        public int manifestSchemaVersion;
        public string exportMode;
        public string source;
        public string sourcePath;
        public float sourceCanvasWidth;
        public float sourceCanvasHeight;
        public string exportedAt;
        public string outputFolder;
        public PSDManifestItem[] items;
        public PSDManifestWarning[] warnings;
    }

    [Serializable]
    public class PSDManifestItem
    {
        public string id;
        public string assetId;
        public string layerId;
        public string assetType;
        public string prefabHint;
        public string sha256;
        public string visualHash;
        public int zIndex;

        public string name;
        public string originalName;
        public string layerPath;
        public string layerType;
        public string variant;
        public string file;
        public string relativeFolder;
        public float x;
        public float y;
        public float width;
        public float height;
        public float originalTrimmedWidth;
        public float originalTrimmedHeight;
        public bool evenSizeApplied;
        public PSDCanvasSize canvasSize;
        public PSDUniformSize uniformSize;
        public PSDNineSlice nineSlice;
        public string sourceType;
    }

    [Serializable]
    public class PSDCanvasSize
    {
        public bool enabled;
        public bool applied;
        public float width;
        public float height;
    }

    [Serializable]
    public class PSDUniformSize
    {
        public bool enabled;
        public bool applied;
        public float requestedWidth;
        public float requestedHeight;
        public float widthBefore;
        public float heightBefore;
        public float width;
        public float height;
        public bool cropped;
    }

    [Serializable]
    public class PSDNineSlice
    {
        public bool enabled;
        public bool applied;
        public PSDNineSliceBorders borders;
        public bool horizontalSlice;
        public bool verticalSlice;
        public float centerSize;
        public float originalWidth;
        public float originalHeight;
        public float width;
        public float height;
    }

    [Serializable]
    public class PSDNineSliceBorders
    {
        public float left;
        public float right;
        public float top;
        public float bottom;
    }

    [Serializable]
    public class PSDManifestWarning
    {
        public string type;
        public string layerPath;
        public string message;
    }

    public static class PSDManifestParser
    {
        public const string ManifestFileName = "export_manifest.json";

        public static PSDManifest LoadFromFolder(string folderPath)
        {
            string manifestPath = Path.Combine(folderPath, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("Could not find export_manifest.json.", manifestPath);
            }

            string json = File.ReadAllText(manifestPath);
            PSDManifest manifest = JsonUtility.FromJson<PSDManifest>(json);
            if (manifest == null)
            {
                throw new InvalidDataException("Failed to parse export_manifest.json.");
            }

            if (manifest.items == null)
            {
                manifest.items = new PSDManifestItem[0];
            }

            return manifest;
        }

        public static Vector2 GetCanvasSize(PSDManifest manifest)
        {
            if (manifest.sourceCanvasWidth > 0f && manifest.sourceCanvasHeight > 0f)
            {
                return new Vector2(manifest.sourceCanvasWidth, manifest.sourceCanvasHeight);
            }

            foreach (PSDManifestItem item in manifest.items)
            {
                if (item.canvasSize != null && item.canvasSize.width > 0f && item.canvasSize.height > 0f)
                {
                    return new Vector2(item.canvasSize.width, item.canvasSize.height);
                }
            }

            float width = 1f;
            float height = 1f;
            foreach (PSDManifestItem item in manifest.items)
            {
                width = Mathf.Max(width, item.x + item.width);
                height = Mathf.Max(height, item.y + item.height);
            }

            return new Vector2(width, height);
        }

        public static List<string> GetSourceWarnings(PSDManifest manifest)
        {
            List<string> results = new List<string>();
            if (manifest.warnings == null)
            {
                return results;
            }

            foreach (PSDManifestWarning warning in manifest.warnings)
            {
                string prefix = string.IsNullOrEmpty(warning.layerPath) ? "" : warning.layerPath + ": ";
                results.Add(prefix + warning.message);
            }

            return results;
        }
    }
}
