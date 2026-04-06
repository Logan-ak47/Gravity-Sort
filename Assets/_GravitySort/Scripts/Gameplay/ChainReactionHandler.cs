using System.Collections.Generic;
using UnityEngine;

namespace GravitySort
{
    public class ChainReactionHandler : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private MatchChecker matchChecker;
        [SerializeField] private GameConfig config;
        [SerializeField] private PourAnimator pourAnimator;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired after each clear step with (totalBlocksCleared, comboStep).
        /// comboStep starts at 1 for the first clear, 2 for the chain, etc.
        /// </summary>
        public event System.Action<int, int> OnBlocksCleared;

        /// <summary>
        /// Fired when the chain is fully resolved with no remaining matches.
        /// Carries the final combo count (0 = no matches were ever found).
        /// </summary>
        public event System.Action<int> OnChainComplete;

        // ── State ──────────────────────────────────────────────────────────────

        private int comboCount;

        // ── API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kicks off the asynchronous chain reaction loop.
        /// Call this after every pour settle completes.
        /// </summary>
        public void StartChainCheck()
        {
            comboCount = 0;
            DoCheckStep();
        }

        // ── Chain loop (callback-driven, never blocks a frame) ─────────────────

        private void DoCheckStep()
        {
            List<MatchResult> matches = matchChecker.CheckAllColumns();

            if (matches.Count == 0)
            {
                OnChainComplete?.Invoke(comboCount);
                return;
            }

            comboCount++;

            // Sort matches so that within each column, higher rows come first.
            // This ensures RemoveBlocksAtRange calls don't invalidate lower-row
            // indices for subsequent removals in the same column.
            matches.Sort((a, b) =>
            {
                if (a.column != b.column) return a.column.CompareTo(b.column);
                return b.startRow.CompareTo(a.startRow); // descending
            });

            // ── Step 1: collect all Block refs BEFORE touching data ────────────
            var blocksToAnimate = new List<Block>();
            int totalCleared    = 0;

            foreach (MatchResult match in matches)
            {
                totalCleared += match.count;
                for (int i = 0; i < match.count; i++)
                {
                    Block b = gridManager.GetBlockVisual(match.column, match.startRow + i);
                    if (b != null) blocksToAnimate.Add(b);
                }
            }

            // ── Step 2: notify listeners ───────────────────────────────────────
            OnBlocksCleared?.Invoke(totalCleared, comboCount);

            // ── Step 3: animate clears → pool blocks → settle → recheck ────────
            //    RemoveBlocksAtRange is called AFTER animations so the clear plays
            //    before blocks are returned to the pool and data is updated.
            if (blocksToAnimate.Count == 0)
            {
                // No visuals to animate — remove data and settle immediately
                foreach (MatchResult match in matches)
                    gridManager.RemoveBlocksAtRange(match.column, match.startRow, match.count);
                gridManager.SettleAllColumns(DoCheckStep);
                return;
            }

            int pending = blocksToAnimate.Count;
            foreach (Block b in blocksToAnimate)
            {
                b.PlayClearAnimation(() =>
                {
                    pending--;
                    if (pending <= 0)
                    {
                        // All clear animations done — remove data + pool, then settle
                        foreach (MatchResult match in matches)
                            gridManager.RemoveBlocksAtRange(match.column, match.startRow, match.count);
                        gridManager.SettleAllColumns(DoCheckStep);
                    }
                });
            }
        }
    }
}
