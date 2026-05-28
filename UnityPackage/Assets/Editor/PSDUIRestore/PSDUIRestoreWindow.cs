using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PSDUIRestore
{
    public class PSDUIRestoreWindow : EditorWindow
    {
        private string exportFolderPath = "";
        private PSDManifest manifest;
        private List<AssetMatch> matches = new List<AssetMatch>();
        private Vector2 scroll;
        private string status = "Select or drag a PSD Auto Slicer export folder inside this Unity project's Assets folder.";

        [MenuItem("Tools/PSD Auto Slicer/Restore UI")]
        public static void Open()
        {
            PSDUIRestoreWindow window = GetWindow<PSDUIRestoreWindow>("PSD UI Restore");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("PSD UI Restore", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawExportFolderField();

            EditorGUILayout.HelpBox(status, MessageType.Info);

            using (new EditorGUI.DisabledScope(manifest == null))
            {
                if (GUILayout.Button("Build UGUI Prefab", GUILayout.Height(32f)))
                {
                    Build();
                }
            }

            EditorGUILayout.Space(8f);
            DrawManifestSummary();
        }

        private void SelectFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("Select PSD Auto Slicer Export Folder", Application.dataPath, "");
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            exportFolderPath = selected;
            LoadManifestAndPreview();
        }

        private void DrawExportFolderField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect fieldRect = EditorGUILayout.GetControlRect();
                EditorGUI.TextField(fieldRect, "Export Folder", exportFolderPath);
                HandleFolderDragAndDrop(fieldRect);

                if (GUILayout.Button("Select", GUILayout.Width(82f)))
                {
                    SelectFolder();
                }
            }
        }

        private void HandleFolderDragAndDrop(Rect dropRect)
        {
            Event current = Event.current;
            if (!dropRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
            {
                return;
            }

            string draggedFolder = GetDraggedFolderPath();
            DragAndDrop.visualMode = string.IsNullOrEmpty(draggedFolder) ? DragAndDropVisualMode.Rejected : DragAndDropVisualMode.Copy;

            if (current.type == EventType.DragPerform && !string.IsNullOrEmpty(draggedFolder))
            {
                DragAndDrop.AcceptDrag();
                exportFolderPath = draggedFolder;
                GUI.FocusControl(null);
                LoadManifestAndPreview();
            }

            current.Use();
        }

        private string GetDraggedFolderPath()
        {
            foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
            {
                string assetPath = AssetDatabase.GetAssetPath(draggedObject);
                string absolutePath = AssetPathToAbsolutePath(assetPath);
                if (Directory.Exists(absolutePath))
                {
                    return absolutePath;
                }
            }

            foreach (string path in DragAndDrop.paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                string absolutePath = path;
                if (path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    absolutePath = AssetPathToAbsolutePath(path);
                }

                if (Directory.Exists(absolutePath))
                {
                    return Path.GetFullPath(absolutePath);
                }
            }

            return "";
        }

        private string AssetPathToAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return "";
            }

            if (!assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private void LoadManifestAndPreview()
        {
            try
            {
                if (!AssetMatcher.IsInsideProjectAssets(exportFolderPath))
                {
                    manifest = null;
                    matches.Clear();
                    status = "The export folder must be inside this Unity project's Assets folder for the MVP, so Unity can import PNGs as Sprites.";
                    return;
                }

                manifest = PSDManifestParser.LoadFromFolder(exportFolderPath);
                matches = AssetMatcher.MatchSprites(exportFolderPath, manifest);
                int found = 0;
                int hashMatched = 0;
                int sameFolderSizeMatched = 0;
                foreach (AssetMatch match in matches)
                {
                    if (match.Found)
                    {
                        found++;
                    }

                    if (match.Mode == AssetMatchMode.Hash)
                    {
                        hashMatched++;
                    }

                    if (match.Mode == AssetMatchMode.SameFolderSize)
                    {
                        sameFolderSizeMatched++;
                    }
                }

                status = $"Loaded manifest {manifest.version}. Matched {found}/{matches.Count} image items. Hash: {hashMatched}, same-folder size: {sameFolderSizeMatched}.";
            }
            catch (Exception error)
            {
                manifest = null;
                matches.Clear();
                status = error.Message;
            }
        }

        private void Build()
        {
            try
            {
                PSDUIBuildResult result = PSDUIBuilder.BuildPrefab(manifest, matches);
                status = $"Generated {result.GeneratedCount} Image nodes, missing {result.MissingCount}. Prefab saved at {result.PrefabPath}.";
            }
            catch (Exception error)
            {
                status = error.Message;
            }
        }

        private void DrawManifestSummary()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (manifest == null)
            {
                EditorGUILayout.LabelField("No manifest loaded.");
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Manifest", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tool", manifest.tool ?? "");
            EditorGUILayout.LabelField("Version", manifest.version ?? "");
            EditorGUILayout.LabelField("Source", manifest.source ?? "");
            EditorGUILayout.LabelField("Items", manifest.items.Length.ToString());

            Vector2 canvasSize = PSDManifestParser.GetCanvasSize(manifest);
            EditorGUILayout.LabelField("Canvas Size", $"{canvasSize.x:0} x {canvasSize.y:0}");

            DrawSourceWarnings();
            DrawMissingItems();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceWarnings()
        {
            List<string> warnings = PSDManifestParser.GetSourceWarnings(manifest);
            if (warnings.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Source Warnings", EditorStyles.boldLabel);
            foreach (string warning in warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawMissingItems()
        {
            if (matches == null || matches.Count == 0)
            {
                return;
            }

            int missing = 0;
            foreach (AssetMatch match in matches)
            {
                if (!match.Found)
                {
                    missing++;
                }
            }

            if (missing == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Missing PNGs", EditorStyles.boldLabel);
            foreach (AssetMatch match in matches)
            {
                if (match.Found)
                {
                    continue;
                }

                PSDManifestItem item = match.Item;
                string name = string.IsNullOrEmpty(item.layerPath) ? item.name : item.layerPath;
                EditorGUILayout.LabelField(string.IsNullOrEmpty(name) ? "(unnamed item)" : name);
            }
        }
    }
}
