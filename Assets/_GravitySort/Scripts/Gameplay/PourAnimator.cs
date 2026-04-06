using DG.Tweening;
using UnityEngine;

namespace GravitySort
{
    public class PourAnimator : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameConfig config;

        // ── API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Animates <paramref name="blockCount"/> pre-grabbed Block objects arcing
        /// into <paramref name="destCol"/>.
        ///
        /// CALL THIS AFTER ExecutePour (data-only) — destCol's columnData already
        /// reflects the incoming blocks, so destStartRow is computed from the
        /// post-pour column height.
        ///
        /// Each block is registered in blockVisuals via SetBlockVisual on landing.
        /// After all blocks land, RefreshVisuals() is called and
        /// <paramref name="onComplete"/> is invoked.
        /// </summary>
        /// <param name="blocks">Block refs grabbed before ExecutePour ran; [0] = topmost.</param>
        /// <param name="blockCount">Number of blocks to animate (may be ≤ blocks.Length).</param>
        /// <param name="destCol">Destination column index.</param>
        public void AnimatePour(Block[] blocks, int blockCount, int destCol,
                                System.Action onComplete)
        {
            // Post-ExecutePour, destCol already has blockCount new entries at the top.
            // The bottom of the incoming group sits at (newHeight - blockCount).
            int destStartRow = gridManager.GetColumnHeight(destCol) - blockCount;

            int completedCount = 0;

            for (int i = 0; i < blockCount; i++)
            {
                // blocks[0] = topmost → lands highest; blocks[blockCount-1] = bottommost → lands lowest.
                int     destRow        = destStartRow + (blockCount - 1 - i);
                Vector3 target         = gridManager.GetWorldPosition(destCol, destRow);
                float   delay          = i * config.pourStagger;

                Block capturedBlock = blocks[i];
                int   capturedRow   = destRow;

                DOTween.Kill(capturedBlock.transform);

                capturedBlock.transform
                    .DOJump(target, config.pourArcHeight, 1, config.pourBlockDuration)
                    .SetEase(Ease.OutQuad)
                    .SetDelay(delay)
                    .OnComplete(() =>
                    {
                        // Register this block in its final visual slot.
                        gridManager.SetBlockVisual(destCol, capturedRow, capturedBlock);

                        // Landing bounce — proportional to block size
                        Vector3 punch = capturedBlock.transform.localScale * 0.12f;
                        capturedBlock.transform.DOPunchScale(punch, 0.15f, 5, 0f);

                        completedCount++;
                        if (completedCount >= blockCount)
                        {
                            // Sync all block visuals to their data positions,
                            // then notify the caller (GameplayController).
                            gridManager.RefreshVisuals();
                            onComplete?.Invoke();
                        }
                    });
            }
        }
    }
}
