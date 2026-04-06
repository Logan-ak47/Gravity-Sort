using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GravitySort
{
    public static class LevelValidator
    {
        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if every color's total block count (startingBlocks +
        /// dropSequence) is divisible by <see cref="GameConfig.matchThreshold"/>.
        /// Only enforced for <see cref="WinConditionType.ClearAll"/> levels.
        /// Logs one warning per offending color.
        /// </summary>
        public static bool ValidateLevel(LevelData level, int matchThreshold = 3)
        {
            if (level.winCondition != WinConditionType.ClearAll)
                return true;

            // Tally total blocks per colorIndex
            var counts = new Dictionary<int, int>();

            foreach (StartingBlock sb in level.startingBlocks)
                AddCount(counts, sb.colorIndex);

            foreach (DropEntry de in level.dropSequence)
                AddCount(counts, de.colorIndex);

            bool valid = true;

            foreach (var kvp in counts)
            {
                if (kvp.Value % matchThreshold != 0)
                {
                    Debug.LogWarning(
                        $"[LevelValidator] '{level.name}': color index {kvp.Key} " +
                        $"has {kvp.Value} block(s) — not divisible by {matchThreshold}.",
                        level);
                    valid = false;
                }
            }

            return valid;
        }

        // ── Menu item ──────────────────────────────────────────────────────────

        [MenuItem("GravitySort/Validate All Levels")]
        private static void ValidateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");

            if (guids.Length == 0)
            {
                Debug.Log("[LevelValidator] No LevelData assets found.");
                return;
            }

            // Load GameConfig to read the real matchThreshold
            int threshold = LoadMatchThreshold();

            int passCount = 0;
            int failCount = 0;

            foreach (string guid in guids)
            {
                string path  = AssetDatabase.GUIDToAssetPath(guid);
                var    level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (level == null) continue;

                if (ValidateLevel(level, threshold))
                {
                    Debug.Log($"[LevelValidator] '{level.name}' ✓ PASS", level);
                    passCount++;
                }
                else
                {
                    // Per-color warnings already logged inside ValidateLevel
                    failCount++;
                }
            }

            if (failCount == 0)
                Debug.Log($"[LevelValidator] All {passCount} level(s) passed ✓");
            else
                Debug.LogWarning(
                    $"[LevelValidator] {failCount} level(s) FAILED, {passCount} passed. " +
                    "See warnings above for details.");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void AddCount(Dictionary<int, int> counts, int colorIndex)
        {
            counts.TryGetValue(colorIndex, out int current);
            counts[colorIndex] = current + 1;
        }

        private static int LoadMatchThreshold()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameConfig");
            if (guids.Length > 0)
            {
                string path   = AssetDatabase.GUIDToAssetPath(guids[0]);
                var    config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                if (config != null)
                    return config.matchThreshold;
            }

            Debug.LogWarning(
                "[LevelValidator] GameConfig asset not found — using default matchThreshold=3.");
            return 3;
        }
    }
}
