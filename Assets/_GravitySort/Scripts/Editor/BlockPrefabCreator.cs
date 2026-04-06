using UnityEditor;
using UnityEngine;

namespace GravitySort.Editor
{
    public static class BlockPrefabCreator
    {
        private const string PrefabPath = "Assets/_GravitySort/Prefabs/Block.prefab";

        [MenuItem("GravitySort/Create Block Prefab")]
        public static void CreateBlockPrefab()
        {
            // ── 1. Create GameObject ───────────────────────────────────────────
            var go = new GameObject("Block");

            // ── 2. SpriteRenderer ──────────────────────────────────────────────
            var sr = go.AddComponent<SpriteRenderer>();

            // Write a 32×32 white PNG to disk, import it as a sprite with
            // pixelsPerUnit = 32 so its native world-space size is exactly 1×1 unit.
            // GridManager then scales each block instance to CellSize * 0.85 at runtime.
            const string spritePath = "Assets/_GravitySort/Sprites/block_white.png";
            System.IO.Directory.CreateDirectory("Assets/_GravitySort/Sprites");

            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(
                System.IO.Path.Combine(Application.dataPath, "../" + spritePath),
                tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(spritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType          = TextureImporterType.Sprite;
            importer.spriteImportMode     = SpriteImportMode.Single;
            importer.spritePixelsPerUnit  = 32f;
            importer.spritePivot          = new Vector2(0.5f, 0.5f);
            importer.filterMode           = FilterMode.Bilinear;
            importer.mipmapEnabled        = false;
            importer.SaveAndReimport();

            // LoadAllAssetsAtPath returns the Texture2D (main) + Sprite (sub-asset)
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(spritePath))
            {
                if (asset is Sprite s) { sr.sprite = s; break; }
            }
            sr.color = Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 1;

            // ── 3. Block script ────────────────────────────────────────────────
            var block = go.AddComponent<Block>();

            // Wire the private [SerializeField] spriteRenderer field via SerializedObject
            var so = new SerializedObject(block);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── 4. Scale ───────────────────────────────────────────────────────
            // Start at identity — GridManager.SetBaseScale() drives all block scaling.
            go.transform.localScale = Vector3.one;

            // ── 5. Save prefab ─────────────────────────────────────────────────
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 6. Remove from scene ───────────────────────────────────────────
            Object.DestroyImmediate(go);

            if (prefab != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"Block prefab created at {PrefabPath}");
            }
            else
            {
                Debug.LogError("BlockPrefabCreator: prefab save failed — check the Prefabs folder exists.");
            }
        }
    }
}
