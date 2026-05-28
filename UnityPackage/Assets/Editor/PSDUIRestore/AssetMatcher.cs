using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PSDUIRestore
{
    public enum AssetMatchMode
    {
        Missing,
        FileName,
        Hash,
        SameFolderSize
    }

    public class AssetMatch
    {
        public PSDManifestItem Item;
        public string AssetPath;
        public AssetMatchMode Mode;
        public Sprite Sprite;
        public bool Found => Sprite != null;
    }

    internal class PngCandidate
    {
        public string AssetPath;
        public string AbsolutePath;
        public string FileName;
        public string RelativeFolder;
        public string Sha256;
        public int Width;
        public int Height;
    }

    public static class AssetMatcher
    {
        public static List<AssetMatch> MatchSprites(string exportFolderPath, PSDManifest manifest)
        {
            string exportAssetFolder = ToAssetPath(exportFolderPath);
            AssetDatabase.Refresh();

            List<PngCandidate> candidates = ScanPngCandidates(exportFolderPath);
            Dictionary<string, PngCandidate> fileNameIndex = BuildFileNameIndex(candidates);
            Dictionary<string, PngCandidate> sha256Index = BuildSha256Index(candidates);
            HashSet<string> claimedAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<AssetMatch> matches = new List<AssetMatch>();

            foreach (PSDManifestItem item in manifest.items)
            {
                AssetMatchMode mode;
                string assetPath = FindAssetPath(
                    exportAssetFolder,
                    candidates,
                    fileNameIndex,
                    sha256Index,
                    claimedAssetPaths,
                    item,
                    out mode
                );
                Sprite sprite = null;

                if (!string.IsNullOrEmpty(assetPath))
                {
                    claimedAssetPaths.Add(assetPath);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    ConfigureTextureAsSprite(assetPath, item);
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                }

                matches.Add(new AssetMatch
                {
                    Item = item,
                    AssetPath = assetPath,
                    Mode = mode,
                    Sprite = sprite
                });
            }

            return matches;
        }

        public static bool IsInsideProjectAssets(string folderPath)
        {
            return !string.IsNullOrEmpty(ToAssetPath(folderPath));
        }

        public static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return "";
            }

            string fullPath = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string assetsPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');

            if (string.Equals(fullPath, assetsPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            if (fullPath.StartsWith(assetsPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + fullPath.Substring(assetsPath.Length);
            }

            return "";
        }

        private static List<PngCandidate> ScanPngCandidates(string exportFolderPath)
        {
            List<PngCandidate> candidates = new List<PngCandidate>();
            if (string.IsNullOrEmpty(exportFolderPath) || !Directory.Exists(exportFolderPath))
            {
                return candidates;
            }

            string root = Path.GetFullPath(exportFolderPath).Replace('\\', '/').TrimEnd('/');
            string[] files = Directory.GetFiles(exportFolderPath, "*.png", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string absolutePath = Path.GetFullPath(file).Replace('\\', '/');
                string assetPath = ToAssetPath(absolutePath);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                int width;
                int height;
                ReadPngSize(absolutePath, out width, out height);

                string parent = Path.GetDirectoryName(absolutePath).Replace('\\', '/');
                string relativeFolder = "";
                if (parent.Length > root.Length)
                {
                    relativeFolder = parent.Substring(root.Length).TrimStart('/');
                }

                candidates.Add(new PngCandidate
                {
                    AssetPath = assetPath,
                    AbsolutePath = absolutePath,
                    FileName = Path.GetFileName(assetPath),
                    RelativeFolder = NormalizeRelativePath(relativeFolder),
                    Sha256 = ComputeSha256(absolutePath),
                    Width = width,
                    Height = height
                });
            }

            return candidates;
        }

        private static Dictionary<string, PngCandidate> BuildFileNameIndex(List<PngCandidate> candidates)
        {
            Dictionary<string, PngCandidate> index = new Dictionary<string, PngCandidate>(StringComparer.OrdinalIgnoreCase);
            foreach (PngCandidate candidate in candidates)
            {
                if (!index.ContainsKey(candidate.FileName))
                {
                    index[candidate.FileName] = candidate;
                }
            }

            return index;
        }

        private static Dictionary<string, PngCandidate> BuildSha256Index(List<PngCandidate> candidates)
        {
            Dictionary<string, PngCandidate> index = new Dictionary<string, PngCandidate>(StringComparer.OrdinalIgnoreCase);
            foreach (PngCandidate candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate.Sha256))
                {
                    continue;
                }

                if (!index.ContainsKey(candidate.Sha256))
                {
                    index[candidate.Sha256] = candidate;
                }
            }

            return index;
        }

        private static string FindAssetPath(
            string exportAssetFolder,
            List<PngCandidate> candidates,
            Dictionary<string, PngCandidate> fileNameIndex,
            Dictionary<string, PngCandidate> sha256Index,
            HashSet<string> claimedAssetPaths,
            PSDManifestItem item,
            out AssetMatchMode mode)
        {
            mode = AssetMatchMode.Missing;
            if (string.IsNullOrEmpty(exportAssetFolder))
            {
                return "";
            }

            string fileName = GetManifestFileName(item);
            if (!string.IsNullOrEmpty(fileName))
            {
                string directPath = exportAssetFolder;
                if (!string.IsNullOrEmpty(item.relativeFolder))
                {
                    directPath += "/" + NormalizeRelativePath(item.relativeFolder);
                }

                directPath += "/" + fileName;
                if (File.Exists(ToAbsolutePath(directPath)))
                {
                    mode = AssetMatchMode.FileName;
                    return directPath;
                }

                if (fileNameIndex.TryGetValue(fileName, out PngCandidate indexedPath))
                {
                    mode = AssetMatchMode.FileName;
                    return indexedPath.AssetPath;
                }
            }

            string itemHash = NormalizeHash(item.sha256);
            if (!string.IsNullOrEmpty(itemHash) && sha256Index.TryGetValue(itemHash, out PngCandidate hashPath))
            {
                mode = AssetMatchMode.Hash;
                return hashPath.AssetPath;
            }

            PngCandidate sameFolderSizeMatch = FindSameFolderSizeMatch(candidates, claimedAssetPaths, item);
            if (sameFolderSizeMatch != null)
            {
                mode = AssetMatchMode.SameFolderSize;
                return sameFolderSizeMatch.AssetPath;
            }

            return "";
        }

        private static PngCandidate FindSameFolderSizeMatch(
            List<PngCandidate> candidates,
            HashSet<string> claimedAssetPaths,
            PSDManifestItem item)
        {
            string itemFolder = NormalizeRelativePath(item.relativeFolder);
            int itemWidth = Mathf.RoundToInt(item.width);
            int itemHeight = Mathf.RoundToInt(item.height);
            if (itemWidth <= 0 || itemHeight <= 0)
            {
                return null;
            }

            PngCandidate match = null;
            int matchCount = 0;
            foreach (PngCandidate candidate in candidates)
            {
                if (claimedAssetPaths.Contains(candidate.AssetPath))
                {
                    continue;
                }

                if (!string.Equals(candidate.RelativeFolder, itemFolder, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (candidate.Width != itemWidth || candidate.Height != itemHeight)
                {
                    continue;
                }

                match = candidate;
                matchCount++;
                if (matchCount > 1)
                {
                    return null;
                }
            }

            return matchCount == 1 ? match : null;
        }

        private static string GetManifestFileName(PSDManifestItem item)
        {
            if (!string.IsNullOrEmpty(item.file))
            {
                return Path.GetFileName(item.file);
            }

            if (!string.IsNullOrEmpty(item.name))
            {
                return item.name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? item.name : item.name + ".png";
            }

            return "";
        }

        private static string NormalizeRelativePath(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Replace('\\', '/').Trim('/');
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, assetPath.Substring("Assets".Length).TrimStart('/')));
        }

        private static string ComputeSha256(string absolutePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(absolutePath))
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(stream);
                    StringBuilder builder = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                    {
                        builder.Append(b.ToString("x2"));
                    }

                    return builder.ToString();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string NormalizeHash(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Trim().ToLowerInvariant();
        }

        private static void ReadPngSize(string absolutePath, out int width, out int height)
        {
            width = 0;
            height = 0;

            try
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(bytes))
                {
                    width = texture.width;
                    height = texture.height;
                }

                UnityEngine.Object.DestroyImmediate(texture);
            }
            catch (Exception)
            {
                width = 0;
                height = 0;
            }
        }

        private static void ConfigureTextureAsSprite(string assetPath, PSDManifestItem item)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    return;
                }
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            Vector4 border = GetSpriteBorder(item);
            if (border != importer.spriteBorder)
            {
                importer.spriteBorder = border;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static Vector4 GetSpriteBorder(PSDManifestItem item)
        {
            if (item.nineSlice == null || item.nineSlice.borders == null || !item.nineSlice.applied)
            {
                return Vector4.zero;
            }

            PSDNineSliceBorders b = item.nineSlice.borders;
            return new Vector4(b.left, b.bottom, b.right, b.top);
        }
    }
}
