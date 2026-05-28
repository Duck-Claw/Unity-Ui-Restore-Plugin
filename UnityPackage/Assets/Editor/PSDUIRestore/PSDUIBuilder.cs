using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PSDUIRestore
{
    public class PSDUIBuildResult
    {
        public GameObject Root;
        public string PrefabPath;
        public int GeneratedCount;
        public int MissingCount;
        public List<string> MissingItems = new List<string>();
    }

    public static class PSDUIBuilder
    {
        private const string GeneratedFolder = "Assets/PSDUIRestore/Generated";
        private static readonly string[] ControlNodeNames =
        {
            "Bg",
            "Top",
            "TopLeft",
            "TopRight",
            "Left",
            "Center",
            "Right",
            "Bottom",
            "BottomLeft",
            "BottomRight"
        };

        public static PSDUIBuildResult BuildPrefab(PSDManifest manifest, List<AssetMatch> matches)
        {
            Vector2 canvasSize = PSDManifestParser.GetCanvasSize(manifest);
            GameObject canvas = CreateCanvasRoot(manifest, canvasSize);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Dictionary<string, Transform> groupCache = CreateControlNodes(canvasRect);
            PSDUIBuildResult result = new PSDUIBuildResult { Root = canvas };

            foreach (AssetMatch match in matches)
            {
                PSDManifestItem item = match.Item;
                Transform parent = GetOrCreateParent(canvasRect, groupCache, item.layerPath);

                if (!match.Found)
                {
                    result.MissingCount++;
                    result.MissingItems.Add(GetReadableItemName(item));
                    continue;
                }

                GameObject imageObject = new GameObject(GetNodeName(item), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(parent, false);

                RectTransform rect = imageObject.GetComponent<RectTransform>();
                ApplyRect(rect, item, canvasSize);

                Image image = imageObject.GetComponent<Image>();
                image.sprite = match.Sprite;
                image.raycastTarget = false;
                if (item.nineSlice != null && item.nineSlice.applied)
                {
                    image.type = Image.Type.Sliced;
                }

                result.GeneratedCount++;
            }

            EnsureGeneratedFolder();
            result.PrefabPath = AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/" + GetPrefabName(manifest) + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(canvas, result.PrefabPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            return result;
        }

        private static GameObject CreateCanvasRoot(PSDManifest manifest, Vector2 canvasSize)
        {
            string name = GetPrefabName(manifest);
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = canvasSize;
            rect.anchoredPosition = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = canvasSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return root;
        }

        private static Dictionary<string, Transform> CreateControlNodes(RectTransform root)
        {
            Dictionary<string, Transform> groupCache = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ControlNodeNames.Length; i++)
            {
                GameObject node = new GameObject(ControlNodeNames[i], typeof(RectTransform));
                node.transform.SetParent(root, false);
                ApplyControlAnchor(node.GetComponent<RectTransform>(), ControlNodeNames[i]);
                node.transform.SetSiblingIndex(i);
                groupCache[ControlNodeNames[i]] = node.transform;
            }

            return groupCache;
        }

        private static void ApplyControlAnchor(RectTransform rect, string nodeName)
        {
            Vector2 min;
            Vector2 max;

            switch (nodeName)
            {
                case "Top":
                    min = new Vector2(0f, 1f);
                    max = new Vector2(1f, 1f);
                    break;
                case "TopLeft":
                    min = new Vector2(0f, 1f);
                    max = new Vector2(0f, 1f);
                    break;
                case "TopRight":
                    min = new Vector2(1f, 1f);
                    max = new Vector2(1f, 1f);
                    break;
                case "Left":
                    min = new Vector2(0f, 0.5f);
                    max = new Vector2(0f, 0.5f);
                    break;
                case "Center":
                    min = Vector2.zero;
                    max = Vector2.one;
                    break;
                case "Right":
                    min = new Vector2(1f, 0.5f);
                    max = new Vector2(1f, 0.5f);
                    break;
                case "Bottom":
                    min = new Vector2(0f, 0f);
                    max = new Vector2(1f, 0f);
                    break;
                case "BottomLeft":
                    min = new Vector2(0f, 0f);
                    max = new Vector2(0f, 0f);
                    break;
                case "BottomRight":
                    min = new Vector2(1f, 0f);
                    max = new Vector2(1f, 0f);
                    break;
                case "Bg":
                default:
                    min = Vector2.zero;
                    max = Vector2.one;
                    break;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Transform GetOrCreateParent(RectTransform root, Dictionary<string, Transform> groupCache, string layerPath)
        {
            string[] groupParts = GetGroupParts(layerPath);
            Transform current = root;
            string key = "";

            foreach (string rawPart in groupParts)
            {
                string part = SanitizeUnityName(rawPart);
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                key = string.IsNullOrEmpty(key) ? part : key + "/" + part;
                if (groupCache.TryGetValue(key, out Transform cached))
                {
                    current = cached;
                    continue;
                }

                GameObject group = new GameObject(part, typeof(RectTransform));
                group.transform.SetParent(current, false);
                RectTransform rect = group.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                current = group.transform;
                groupCache[key] = current;
            }

            return current;
        }

        private static string[] GetGroupParts(string layerPath)
        {
            if (string.IsNullOrEmpty(layerPath))
            {
                return new string[0];
            }

            string[] parts = layerPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
            {
                return new string[0];
            }

            string[] groups = new string[parts.Length - 1];
            Array.Copy(parts, groups, groups.Length);
            return groups;
        }

        private static void ApplyRect(RectTransform rect, PSDManifestItem item, Vector2 canvasSize)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(item.width, item.height);
            rect.anchoredPosition = new Vector2(
                item.x + item.width * 0.5f - canvasSize.x * 0.5f,
                canvasSize.y * 0.5f - item.y - item.height * 0.5f
            );
        }

        private static string GetPrefabName(PSDManifest manifest)
        {
            string sourceName = string.IsNullOrEmpty(manifest.source) ? "PSD_Restored_UI" : Path.GetFileNameWithoutExtension(manifest.source);
            return SanitizeUnityName(sourceName);
        }

        private static string GetNodeName(PSDManifestItem item)
        {
            if (!string.IsNullOrEmpty(item.name))
            {
                return SanitizeUnityName(Path.GetFileNameWithoutExtension(item.name));
            }

            if (!string.IsNullOrEmpty(item.originalName))
            {
                return SanitizeUnityName(item.originalName);
            }

            return "Image";
        }

        private static string GetReadableItemName(PSDManifestItem item)
        {
            string path = string.IsNullOrEmpty(item.layerPath) ? item.name : item.layerPath;
            return string.IsNullOrEmpty(path) ? "(unnamed item)" : path;
        }

        private static string SanitizeUnityName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Node";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }

        private static void EnsureGeneratedFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PSDUIRestore"))
            {
                AssetDatabase.CreateFolder("Assets", "PSDUIRestore");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets/PSDUIRestore", "Generated");
            }
        }
    }
}
