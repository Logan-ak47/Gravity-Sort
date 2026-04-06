using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Editor tool: GravitySort → Generate All Levels
    /// Batch-creates 30 LevelData ScriptableObjects in Assets/_GravitySort/ScriptableObjects/Levels/.
    ///
    /// Divisibility invariant (ClearAll levels):
    ///   (startPerColor + dropsPerColor) % matchThreshold == 0  (matchThreshold = 3)
    /// This guarantees every color group is mathematically clearable.
    /// </summary>
    public static class LevelGenerator
    {
        private const string OutputPath = "Assets/_GravitySort/ScriptableObjects/Levels";
        private const int MatchThreshold = 3;

        // ── Tier spec ───────────────────────────────────────────────────────────

        private struct TierSpec
        {
            public int    columns;
            public int    colorCount;
            public float  dropInterval;
            public int    startPerColor;   // starting blocks of each color
            public int    dropsPerColor;   // drops of each color
            public int    maxHeight;
            public WinConditionType winCondition;
            public int    winTarget;       // only used by ReduceBelow
            public ArrangementStyle arrangement;
        }

        private enum ArrangementStyle { Sorted, Mixed, Scrambled }

        // Pre-verified: (startPerColor + dropsPerColor) % 3 == 0 for all ClearAll tiers.
        private static TierSpec GetSpec(int level)
        {
            if (level == 1) return new TierSpec
            {
                columns = 3, colorCount = 2, dropInterval = 999f,
                startPerColor = 3, dropsPerColor = 0,        // 3+0=3 ✓
                maxHeight = 8, winCondition = WinConditionType.ClearAll,
                arrangement = ArrangementStyle.Mixed          // Mixed so blocks aren't pre-sorted
            };
            if (level == 2) return new TierSpec
            {
                columns = 4, colorCount = 2, dropInterval = 5f,
                startPerColor = 3, dropsPerColor = 3,        // 3+3=6 ✓
                maxHeight = 8, winCondition = WinConditionType.ClearAll,
                arrangement = ArrangementStyle.Mixed          // introduces drops without pre-solving
            };
            if (level == 3) return new TierSpec
            {
                columns = 5, colorCount = 3, dropInterval = 5f,
                startPerColor = 3, dropsPerColor = 6,        // 3+6=9 ✓
                maxHeight = 8, winCondition = WinConditionType.ClearAll,
                arrangement = ArrangementStyle.Mixed
            };
            if (level <= 10) return new TierSpec
            {
                columns = 6, colorCount = 3, dropInterval = 4f,
                startPerColor = 4, dropsPerColor = 5,        // 4+5=9 ✓
                maxHeight = 8, winCondition = WinConditionType.ClearAll,
                arrangement = ArrangementStyle.Mixed
            };
            if (level <= 20)
            {
                bool useReduceBelow = (level % 3 == 0);
                int  colors = (level <= 15) ? 3 : 4;
                int  startPer = (colors == 3) ? 5 : 4;     // 5+7=12✓ / 4+5=9✓
                int  dropsPer = (colors == 3) ? 7 : 5;
                return new TierSpec
                {
                    columns = 6, colorCount = colors,
                    dropInterval = Mathf.Lerp(3.5f, 3.0f, (level - 11) / 9f),
                    startPerColor = startPer, dropsPerColor = dropsPer,
                    maxHeight = 8,
                    winCondition = useReduceBelow
                        ? WinConditionType.ReduceBelow
                        : WinConditionType.ClearAll,
                    winTarget = 5,
                    arrangement = ArrangementStyle.Mixed
                };
            }
            // Hard: levels 21-30
            {
                int colors = (level <= 25) ? 4 : 5;
                int startPer = (colors == 4) ? 5 : 4;       // 5+7=12✓ / 4+5=9✓
                int dropsPer = (colors == 4) ? 7 : 5;
                return new TierSpec
                {
                    columns = 6, colorCount = colors,
                    dropInterval = Mathf.Lerp(2.5f, 2.2f, (level - 21) / 9f),
                    startPerColor = startPer, dropsPerColor = dropsPer,
                    maxHeight = 8, winCondition = WinConditionType.ClearAll,
                    arrangement = ArrangementStyle.Scrambled
                };
            }
        }

        // ── Menu entry ──────────────────────────────────────────────────────────

        [MenuItem("GravitySort/Generate All Levels")]
        public static void GenerateAllLevels()
        {
            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(OutputPath))
            {
                string parent = "Assets/_GravitySort/ScriptableObjects";
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets/_GravitySort", "ScriptableObjects");
                AssetDatabase.CreateFolder(parent, "Levels");
            }

            for (int lvl = 1; lvl <= 30; lvl++)
                GenerateLevel(lvl);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Auto-populate LevelManager.levels[] in the scene so the user doesn't
            // need to manually drag 30 assets into the Inspector.
            AutoWireLevelManager();

            Debug.Log("[LevelGenerator] Generated 30 levels in " + OutputPath);
        }

        // ── Per-level generation ────────────────────────────────────────────────

        private static void GenerateLevel(int levelNumber)
        {
            TierSpec spec = GetSpec(levelNumber);

            // Seed for deterministic output
            Random.InitState(levelNumber);

            // Build starting blocks
            StartingBlock[] starting = BuildStartingBlocks(spec);

            // Simulate grid state after placing starting blocks (for helpful drop bias)
            int[] simHeights  = new int[spec.columns];
            int[] simTopColor = new int[spec.columns];
            for (int c = 0; c < spec.columns; c++) simTopColor[c] = -1;

            foreach (var sb in starting)
            {
                simHeights[sb.column]++;
                simTopColor[sb.column] = sb.colorIndex;
            }

            // Build drop sequence
            DropEntry[] drops = BuildDropSequence(spec, simHeights, simTopColor);

            // Create or overwrite the asset
            string assetPath = $"{OutputPath}/Level_{levelNumber:D2}.asset";
            string existing = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(existing))
                AssetDatabase.DeleteAsset(assetPath);

            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.levelNumber    = levelNumber;
            data.columnCount    = spec.columns;
            data.maxHeight      = spec.maxHeight;
            data.colorCount     = spec.colorCount;
            data.dropInterval   = spec.dropInterval;
            data.winCondition   = spec.winCondition;
            data.winTarget      = spec.winTarget;
            data.startingBlocks = starting;
            data.dropSequence   = drops;
            data.maxDrops       = drops.Length;

            AssetDatabase.CreateAsset(data, assetPath);
        }

        // ── Starting block arrangements ─────────────────────────────────────────

        private static StartingBlock[] BuildStartingBlocks(TierSpec spec)
        {
            // Build a flat list: spec.startPerColor copies of each color
            var colorList = new List<int>();
            for (int ci = 0; ci < spec.colorCount; ci++)
                for (int n = 0; n < spec.startPerColor; n++)
                    colorList.Add(ci);

            // Distribute into columns based on arrangement style
            var blocks = new List<StartingBlock>();

            switch (spec.arrangement)
            {
                case ArrangementStyle.Sorted:
                    AppendSorted(blocks, colorList, spec);
                    break;

                case ArrangementStyle.Mixed:
                    AppendMixed(blocks, colorList, spec);
                    break;

                case ArrangementStyle.Scrambled:
                    AppendScrambled(blocks, colorList, spec);
                    break;
            }

            return blocks.ToArray();
        }

        // One color per column, filled bottom-up (tutorial feel)
        private static void AppendSorted(List<StartingBlock> blocks, List<int> colorList, TierSpec spec)
        {
            int ci = 0;
            int col = 0;
            while (ci < colorList.Count && col < spec.columns)
            {
                int color = colorList[ci];
                // Stack same-color blocks in this column
                int count = 0;
                while (count < spec.startPerColor && col < spec.columns)
                {
                    blocks.Add(new StartingBlock { column = col, colorIndex = color });
                    count++;
                    if (count == spec.startPerColor) col++;
                }
                ci += spec.startPerColor;
            }
        }

        // Round-robin across columns (interleaved colors per column)
        private static void AppendMixed(List<StartingBlock> blocks, List<int> colorList, TierSpec spec)
        {
            // Shuffle and distribute round-robin across columns
            Shuffle(colorList);
            for (int i = 0; i < colorList.Count; i++)
                blocks.Add(new StartingBlock { column = i % spec.columns, colorIndex = colorList[i] });
        }

        // Full shuffle + skew heights so some columns are taller (harder puzzle)
        private static void AppendScrambled(List<StartingBlock> blocks, List<int> colorList, TierSpec spec)
        {
            Shuffle(colorList);

            // Track heights to enforce maxHeight
            int[] heights = new int[spec.columns];

            // Skew: first ~40% of blocks go into first half of columns
            int half = spec.columns / 2;
            for (int i = 0; i < colorList.Count; i++)
            {
                int col;
                float skewChance = (i < colorList.Count * 0.4f) ? 0.7f : 0.3f;
                if (Random.value < skewChance)
                    col = Random.Range(0, half);
                else
                    col = Random.Range(half, spec.columns);

                // Avoid overflow — find the first column that fits
                if (heights[col] >= spec.maxHeight - 2)
                {
                    col = FindShortestColumn(heights, spec.columns);
                }

                blocks.Add(new StartingBlock { column = col, colorIndex = colorList[i] });
                heights[col]++;
            }
        }

        // ── Drop sequence ───────────────────────────────────────────────────────

        private static DropEntry[] BuildDropSequence(TierSpec spec, int[] simHeights, int[] simTopColor)
        {
            if (spec.dropsPerColor == 0) return new DropEntry[0];

            // Build flat list: spec.dropsPerColor of each color
            var dropColors = new List<int>();
            for (int ci = 0; ci < spec.colorCount; ci++)
                for (int n = 0; n < spec.dropsPerColor; n++)
                    dropColors.Add(ci);

            Shuffle(dropColors);

            var entries = new List<DropEntry>();
            // Clone sim arrays (we update them as drops are assigned)
            int[] heights  = (int[])simHeights.Clone();
            int[] topColor = (int[])simTopColor.Clone();

            foreach (int color in dropColors)
            {
                int col = PickDropColumn(color, heights, topColor, spec);
                entries.Add(new DropEntry { column = col, colorIndex = color });

                // Update sim state
                heights[col]++;
                topColor[col] = color;
            }

            return entries.ToArray();
        }

        // 40% helpful (matching top), 60% random non-full column
        private static int PickDropColumn(int color, int[] heights, int[] topColor, TierSpec spec)
        {
            int maxH = spec.maxHeight;

            if (Random.value < 0.4f)
            {
                // Try to find a column whose top matches this color and isn't full
                var helpful = new List<int>();
                for (int c = 0; c < spec.columns; c++)
                    if (heights[c] < maxH && topColor[c] == color)
                        helpful.Add(c);

                if (helpful.Count > 0)
                    return helpful[Random.Range(0, helpful.Count)];
            }

            // Random non-full column
            var available = new List<int>();
            for (int c = 0; c < spec.columns; c++)
                if (heights[c] < maxH)
                    available.Add(c);

            // Fallback: if every column is full (shouldn't happen with correct spec), use col 0
            if (available.Count == 0) return 0;
            return available[Random.Range(0, available.Count)];
        }

        // ── Utilities ───────────────────────────────────────────────────────────

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static int FindShortestColumn(int[] heights, int columns)
        {
            int minH = int.MaxValue, minCol = 0;
            for (int c = 0; c < columns; c++)
            {
                if (heights[c] < minH) { minH = heights[c]; minCol = c; }
            }
            return minCol;
        }

        // ── Scene wiring ────────────────────────────────────────────────────────

        private static void AutoWireLevelManager()
        {
            // FindObjectOfType works for active objects; LevelManager GO is always active.
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm == null)
            {
                Debug.LogWarning("[LevelGenerator] LevelManager not found in scene — " +
                                 "add it to the scene and run Generate All Levels again.");
                return;
            }

            var so         = new SerializedObject(lm);
            var levelsProp = so.FindProperty("levels");
            levelsProp.arraySize = 30;

            for (int i = 0; i < 30; i++)
            {
                string path  = $"{OutputPath}/Level_{(i + 1):D2}.asset";
                var    asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(lm);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[LevelGenerator] Auto-populated LevelManager.levels[] with 30 assets.");
        }
    }
}
