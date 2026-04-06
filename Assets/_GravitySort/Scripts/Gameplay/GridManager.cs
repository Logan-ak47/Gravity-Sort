using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GravitySort
{
    public class GridManager : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GameConfig config;
        [SerializeField] private Block blockPrefab;

        // ── Grid Settings ──────────────────────────────────────────────────────

        [Header("Grid Settings")]
        [SerializeField] private int columnCount = 6;
        [SerializeField] private int maxRows = 8;

        // ── Internal Data ──────────────────────────────────────────────────────

        // columnData[c][r] = colorIndex; index 0 = bottom of column
        private List<int>[] columnData;

        // ── Object Pool ────────────────────────────────────────────────────────

        private List<Block> pool;

        // ── Visual Tracking ────────────────────────────────────────────────────

        // blockVisuals[column, row] = the active Block component at that cell
        private Block[,] blockVisuals;

        // ── Initialization ─────────────────────────────────────────────────────

        /// <summary>
        /// Sets up column data arrays and pre-instantiates the block pool.
        /// Call this from the level loader before spawning any blocks.
        /// </summary>
        public void InitGrid(int columns, int maxHeight)
        {
            columnCount = columns;
            maxRows     = maxHeight;

            // Data arrays — one List per column, pre-capacitied
            columnData = new List<int>[columnCount];
            for (int c = 0; c < columnCount; c++)
                columnData[c] = new List<int>(maxRows);

            // Visual tracking grid
            blockVisuals = new Block[columnCount, maxRows];

            // Object pool — enough for a completely full board
            int poolSize = columnCount * maxRows;
            pool = new List<Block>(poolSize);
            for (int i = 0; i < poolSize; i++)
            {
                Block b = Instantiate(blockPrefab, transform);
                b.Init(config);
                b.ResetBlock(); // inactive until needed
                pool.Add(b);
            }
        }

        // ── World Positioning ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the world-space center position for a given (column, row) cell.
        /// Derives all measurements from Camera.main so the grid is always
        /// centered on screen regardless of resolution.
        /// </summary>
        public Vector3 GetWorldPosition(int column, int row)
        {
            float camHeight   = Camera.main.orthographicSize * 2f;
            float camWidth    = camHeight * Camera.main.aspect;

            // Usable width after symmetric horizontal padding
            float usableWidth = camWidth * (1f - config.gridPaddingPercent);
            float cellSize    = usableWidth / columnCount;

            // Grid is horizontally centered on camera (origin = screen center)
            float gridLeft = -(usableWidth * 0.5f);
            float x        = gridLeft + column * cellSize + cellSize * 0.5f;

            // Rows stack upward from the configured bottom offset
            float y = config.gridBottomOffset + row * cellSize + cellSize * 0.5f;

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// World-space size of one grid cell, recomputed from Camera.main each access.
        /// Other systems (BlockDropper, PourAnimator, etc.) can read this directly.
        /// </summary>
        public float CellSize
        {
            get
            {
                float camHeight   = Camera.main.orthographicSize * 2f;
                float camWidth    = camHeight * Camera.main.aspect;
                float usableWidth = camWidth * (1f - config.gridPaddingPercent);
                return usableWidth / columnCount;
            }
        }

        // ── Spawning ───────────────────────────────────────────────────────────

        /// <summary>
        /// Pulls a Block from the pool, sizes, colors, and positions it,
        /// then registers it in blockVisuals at (column, row).
        /// </summary>
        public void SpawnBlockAt(int column, int row, int colorIndex)
        {
            Block b = GetFromPool();

            b.SetColor(colorIndex, config);
            b.SetBaseScale(CellSize * 0.85f);
            b.transform.position = GetWorldPosition(column, row);
            b.gameObject.SetActive(true);

            blockVisuals[column, row] = b;
        }

        /// <summary>
        /// Reads startingBlocks from LevelData and populates columnData + visuals.
        /// Blocks are pushed onto their column in array order (bottom-up).
        /// </summary>
        public void SpawnInitialBlocks(StartingBlock[] startingBlocks)
        {
            foreach (StartingBlock sb in startingBlocks)
            {
                int row = columnData[sb.column].Count;
                columnData[sb.column].Add(sb.colorIndex);
                SpawnBlockAt(sb.column, row, sb.colorIndex);
            }
        }

        // ── Column Queries ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the colorIndex and block count of the contiguous same-color
        /// group at the top of the column. Returns (-1, 0) if the column is empty.
        /// </summary>
        public (int colorIndex, int count) GetTopColorGroup(int column)
        {
            List<int> col = columnData[column];
            if (col.Count == 0)
                return (-1, 0);

            int topColor = col[col.Count - 1];
            int count    = 0;
            for (int i = col.Count - 1; i >= 0; i--)
            {
                if (col[i] == topColor) count++;
                else break;
            }
            return (topColor, count);
        }

        /// <summary>
        /// Returns the column index that contains the given world-space X coordinate,
        /// or -1 if X falls outside the grid boundaries. Used by InputHandler.
        /// </summary>
        public int GetColumnForWorldX(float worldX)
        {
            float camHeight   = Camera.main.orthographicSize * 2f;
            float camWidth    = camHeight * Camera.main.aspect;
            float usableWidth = camWidth * (1f - config.gridPaddingPercent);
            float cellSize    = usableWidth / columnCount;
            float gridLeft    = -(usableWidth * 0.5f);

            int col = Mathf.FloorToInt((worldX - gridLeft) / cellSize);
            return (col >= 0 && col < columnCount) ? col : -1;
        }

        /// <summary>
        /// Returns the Block components that form the contiguous same-color group
        /// at the top of the column. Returns an empty array if the column is empty.
        /// Used by GameplayController to drive selection visuals.
        /// </summary>
        public Block[] GetTopColorGroupBlocks(int column)
        {
            var (_, count) = GetTopColorGroup(column);
            if (count == 0) return System.Array.Empty<Block>();

            var blocks = new Block[count];
            int height = columnData[column].Count;
            for (int i = 0; i < count; i++)
                blocks[i] = blockVisuals[column, height - 1 - i];
            return blocks;
        }

        /// <summary>Maximum number of blocks a column can hold.</summary>
        public int MaxRows => maxRows;

        /// <summary>Total number of columns in the grid.</summary>
        public int ColumnCount => columnCount;

        /// <summary>
        /// Returns the colorIndex of the block at (column, row), or -1 if out of range.
        /// Row 0 = bottom of column.
        /// </summary>
        public int GetColorAt(int column, int row)
        {
            List<int> col = columnData[column];
            return (row >= 0 && row < col.Count) ? col[row] : -1;
        }

        /// <summary>Returns the Block visual at (column, row), or null if the slot is empty.</summary>
        public Block GetBlockVisual(int column, int row) => blockVisuals[column, row];

        /// <summary>Returns the number of blocks currently in the column.</summary>
        public int GetColumnHeight(int column) => columnData[column].Count;

        /// <summary>Returns the height of the tallest column.</summary>
        public int GetMaxColumnHeight()
        {
            if (columnData == null) return 0;
            int max = 0;
            for (int c = 0; c < columnCount; c++)
            {
                int h = columnData[c].Count;
                if (h > max) max = h;
            }
            return max;
        }

        /// <summary>
        /// Returns the indices of every column whose height is >= <paramref name="dangerRow"/>.
        /// </summary>
        public List<int> GetDangerColumns(int dangerRow)
        {
            if (columnData == null) return new List<int>();
            var result = new List<int>();
            for (int c = 0; c < columnCount; c++)
            {
                if (columnData[c].Count >= dangerRow)
                    result.Add(c);
            }
            return result;
        }

        /// <summary>Returns true when the column has reached maxRows capacity.</summary>
        public bool IsColumnFull(int column) => columnData[column].Count >= maxRows;

        /// <summary>Returns true when the column contains no blocks.</summary>
        public bool IsColumnEmpty(int column) => columnData[column].Count == 0;

        /// <summary>
        /// Returns the colorIndex of the topmost block, or -1 if the column is empty.
        /// </summary>
        public int GetTopColor(int column)
        {
            List<int> col = columnData[column];
            return col.Count > 0 ? col[col.Count - 1] : -1;
        }

        // ── Data Mutation + Visual Sync ────────────────────────────────────────

        /// <summary>
        /// Pushes a block onto the top of a column — updates columnData and
        /// spawns the corresponding Block visual.
        /// </summary>
        public void AddBlockToColumn(int column, int colorIndex)
        {
            int row = columnData[column].Count;
            columnData[column].Add(colorIndex);
            SpawnBlockAt(column, row, colorIndex);
        }

        /// <summary>
        /// Removes <paramref name="count"/> blocks from the top of a column,
        /// returns their visuals to the pool, and clears blockVisuals entries.
        /// </summary>
        public void RemoveBlocksFromTop(int column, int count)
        {
            List<int> col       = columnData[column];
            int       toRemove  = Mathf.Min(count, col.Count);

            for (int i = 0; i < toRemove; i++)
            {
                int row = col.Count - 1;

                Block b = blockVisuals[column, row];
                if (b != null)
                {
                    b.ResetBlock();
                    blockVisuals[column, row] = null;
                }

                col.RemoveAt(row);
            }
        }

        // ── Visuals ────────────────────────────────────────────────────────────

        /// <summary>
        /// Repositions and resizes every active Block visual to match its data cell.
        /// Call this after pours and gravity settles.
        /// </summary>
        public void RefreshVisuals()
        {
            float size = CellSize * 0.85f;

            for (int c = 0; c < columnCount; c++)
            {
                for (int r = 0; r < columnData[c].Count; r++)
                {
                    Block b = blockVisuals[c, r];
                    if (b == null) continue;

                    b.SetBaseScale(size);
                    b.transform.position = GetWorldPosition(c, r);
                }
            }
        }

        // ── Range Removal (for match clearing) ────────────────────────────────

        /// <summary>
        /// Removes <paramref name="count"/> blocks starting at <paramref name="startRow"/>
        /// from columnData, pools their Block visuals via ResetBlock, and shifts
        /// the remaining blockVisuals slots downward to close the gap.
        ///
        /// Call this AFTER clear animations have finished (PlayClearAnimation already
        /// calls ResetBlock internally, so this is a safe idempotent pool-back).
        /// Do NOT call SettleAllColumns from here — let the caller do that so the
        /// clear animation plays before any gravity settle.
        /// </summary>
        public void RemoveBlocksAtRange(int column, int startRow, int count)
        {
            List<int> col      = columnData[column];
            int       toRemove = Mathf.Min(count, col.Count - startRow);
            if (toRemove <= 0) return;

            int oldHeight = col.Count;

            // Pool and clear the visual slots in the removed range
            for (int i = 0; i < toRemove; i++)
            {
                int   row = startRow + i;
                Block b   = blockVisuals[column, row];
                if (b != null)
                {
                    b.ResetBlock();
                    blockVisuals[column, row] = null;
                }
            }

            // Shift remaining slots downward to fill the gap
            for (int r = startRow + toRemove; r < oldHeight; r++)
            {
                blockVisuals[column, r - toRemove] = blockVisuals[column, r];
                blockVisuals[column, r]             = null;
            }

            col.RemoveRange(startRow, toRemove);
        }

        // ── Data-Only Mutation (for animation pipeline) ────────────────────────

        /// <summary>
        /// Removes <paramref name="count"/> entries from the top of columnData and
        /// clears the corresponding blockVisuals slots — but does NOT deactivate
        /// the Block objects. Call this before animating the blocks to their destination.
        /// </summary>
        public void RemoveBlocksFromTopDataOnly(int column, int count)
        {
            List<int> col      = columnData[column];
            int       toRemove = Mathf.Min(count, col.Count);

            for (int i = 0; i < toRemove; i++)
            {
                int row = col.Count - 1;
                blockVisuals[column, row] = null;
                col.RemoveAt(row);
            }
        }

        /// <summary>
        /// Appends a color entry to columnData without spawning a visual.
        /// Call this before SetBlockVisual to pre-register the destination slot.
        /// </summary>
        public void AddBlockToColumnData(int column, int colorIndex)
        {
            columnData[column].Add(colorIndex);
        }

        /// <summary>
        /// Assigns an existing Block to blockVisuals[column, row].
        /// Used by PourAnimator to register an in-flight block at its landing cell.
        /// </summary>
        public void SetBlockVisual(int column, int row, Block block)
        {
            blockVisuals[column, row] = block;
        }

        // ── Settle Animations ──────────────────────────────────────────────────

        /// <summary>
        /// Animates any block in <paramref name="column"/> that is visually above
        /// its correct data position downward to the right spot.
        /// Blocks are compared by world position; only those that need to move
        /// get a tween. Calls <paramref name="onComplete"/> when all tweens finish
        /// (or immediately if nothing needs to move).
        /// </summary>
        public void SettleColumn(int column, System.Action onComplete)
        {
            List<int> col = columnData[column];

            int pending = 0;

            for (int r = 0; r < col.Count; r++)
            {
                Block b = blockVisuals[column, r];
                if (b == null) continue;

                Vector3 correct = GetWorldPosition(column, r);
                if (b.transform.position == correct) continue;

                pending++;

                Block  captured = b;
                DOTween.Kill(captured.transform);
                captured.transform
                    .DOMove(correct, config.settleDuration)
                    .SetEase(Ease.OutBounce)
                    .OnComplete(() =>
                    {
                        pending--;
                        if (pending <= 0)
                            onComplete?.Invoke();
                    });
            }

            if (pending == 0)
                onComplete?.Invoke();
        }

        /// <summary>
        /// Runs <see cref="SettleColumn"/> on every column in parallel and calls
        /// <paramref name="onComplete"/> once ALL columns have finished settling.
        /// If no block in any column needs to move, calls onComplete immediately.
        /// </summary>
        public void SettleAllColumns(System.Action onComplete)
        {
            int remaining = columnCount;

            for (int c = 0; c < columnCount; c++)
            {
                int captured = c;
                SettleColumn(captured, () =>
                {
                    remaining--;
                    if (remaining <= 0)
                        onComplete?.Invoke();
                });
            }
        }

        // ── Pool Helpers ───────────────────────────────────────────────────────

        private Block GetFromPool()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].gameObject.activeSelf)
                    return pool[i];
            }

            // Pool exhausted — grow gracefully (should not happen in normal play)
            Block extra = Instantiate(blockPrefab, transform);
            extra.Init(config);
            pool.Add(extra);
            return extra;
        }
    }
}
