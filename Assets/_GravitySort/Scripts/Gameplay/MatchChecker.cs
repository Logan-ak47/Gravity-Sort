using System.Collections.Generic;
using UnityEngine;

namespace GravitySort
{
    [System.Serializable]
    public struct MatchResult
    {
        public int column;
        public int startRow;   // bottom row of the matched group (inclusive)
        public int count;
        public int colorIndex;
    }

    public class MatchChecker : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameConfig config;

        // ── API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans every column from bottom to top and returns all contiguous
        /// same-color groups whose size is >= <see cref="GameConfig.matchThreshold"/>.
        /// Returns an empty list if no matches exist.
        /// </summary>
        public List<MatchResult> CheckAllColumns()
        {
            var results = new List<MatchResult>();

            for (int c = 0; c < gridManager.ColumnCount; c++)
            {
                int height = gridManager.GetColumnHeight(c);
                if (height == 0) continue;

                int r = 0;
                while (r < height)
                {
                    int groupColor = gridManager.GetColorAt(c, r);
                    int groupStart = r;
                    int groupCount = 0;

                    // Walk upward while the color matches
                    while (r < height && gridManager.GetColorAt(c, r) == groupColor)
                    {
                        groupCount++;
                        r++;
                    }

                    // Only clear a multiple of matchThreshold so the divisibility
                    // invariant holds even when drops stack more than threshold blocks.
                    int clearCount = (groupCount / config.matchThreshold) * config.matchThreshold;
                    if (clearCount >= config.matchThreshold)
                    {
                        results.Add(new MatchResult
                        {
                            column     = c,
                            startRow   = groupStart,
                            count      = clearCount,
                            colorIndex = groupColor,
                        });
                    }
                }
            }

            return results;
        }
    }
}
