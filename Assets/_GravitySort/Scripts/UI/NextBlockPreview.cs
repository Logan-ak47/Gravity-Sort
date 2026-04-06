using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Displays the next N upcoming drops above the grid.
    /// Each slot shows a small colored square at the target column's X position,
    /// plus a thin vertical line connecting it to the game-over line so the
    /// player knows which column will receive the block.
    ///
    /// Attach to any persistent GameObject (e.g. the Manager GameObject).
    /// Assign blockSprite to the same block_white.png used by the Block prefab.
    /// </summary>
    public class NextBlockPreview : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private BlockDropper blockDropper;
        [SerializeField] private GridManager  gridManager;
        [SerializeField] private GameConfig   config;
        [SerializeField] private Sprite       blockSprite;  // block_white.png

        // ── Config ─────────────────────────────────────────────────────────────

        /// <summary>Preview blocks are rendered at this fraction of the game block size.</summary>
        private const float PreviewScale  = 0.5f;

        /// <summary>How many cells above the game-over line to center the preview row.</summary>
        private const float RowsAboveTop  = 0.6f;

        /// <summary>Sorting order for preview block sprites.</summary>
        private const int   BlockSortOrder = 5;

        /// <summary>Sorting order for the column indicator lines.</summary>
        private const int   LineSortOrder  = 4;

        // ── Internals ──────────────────────────────────────────────────────────

        private SpriteRenderer[] previewBlocks;
        private SpriteRenderer[] columnLines;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            int count     = config.previewCount;
            previewBlocks = new SpriteRenderer[count];
            columnLines   = new SpriteRenderer[count];

            for (int i = 0; i < count; i++)
            {
                previewBlocks[i]       = CreateSprite($"PreviewBlock_{i}", BlockSortOrder);
                columnLines[i]         = CreateSprite($"PreviewLine_{i}",  LineSortOrder);
                columnLines[i].color   = new Color(1f, 1f, 1f, 0.25f);
            }
        }

        private void OnEnable()
        {
            //blockDropper.OnDropDispatched   += OnDrop;
            blockDropper.OnAllDropsComplete += HideAll;
        }

        private void OnDisable()
        {
           // blockDropper.OnDropDispatched   -= OnDrop;
            blockDropper.OnAllDropsComplete -= HideAll;
        }

        private void Start()
        {
            // Show initial upcoming drops as soon as the level is loaded
            Refresh();
        }

        // ── Event callbacks ────────────────────────────────────────────────────

        private void OnDrop(int _) => Refresh();

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Reads GetUpcomingDrops and updates all preview slots.
        /// Call after SetLevel() if you want the preview visible before the first drop.
        /// </summary>
        public void Refresh()
        {
            DropEntry[] upcoming = blockDropper.GetUpcomingDrops(config.previewCount);

            float cellSize  = gridManager.CellSize;
            float blockSize = cellSize * 0.85f * PreviewScale;

            // Y of the cell one row above the last valid row (the "game-over line" center)
            float gameOverLineY = gridManager.GetWorldPosition(0, gridManager.MaxRows).y;
            // Center the preview row above that
            float previewY      = gameOverLineY + cellSize * RowsAboveTop;

            for (int i = 0; i < previewBlocks.Length; i++)
            {
                if (i >= upcoming.Length)
                {
                    previewBlocks[i].gameObject.SetActive(false);
                    columnLines[i].gameObject.SetActive(false);
                    continue;
                }

                DropEntry entry = upcoming[i];

                // X is the horizontal center of the target column
                float x = gridManager.GetWorldPosition(entry.column, 0).x;

                // ── Preview block ──────────────────────────────────────────────
                previewBlocks[i].color                = config.blockColors[entry.colorIndex];
                previewBlocks[i].transform.position   = new Vector3(x, previewY, 0f);
                previewBlocks[i].transform.localScale  = Vector3.one * blockSize;
                previewBlocks[i].gameObject.SetActive(true);

                // ── Column indicator line ──────────────────────────────────────
                // Runs from just below the preview block down to the game-over line.
                // Width is ~15% of blockSize so it reads as a narrow guide, not a block.
                float lineTop    = previewY - blockSize * 0.55f;   // bottom edge of preview block
                float lineBot    = gameOverLineY;
                float lineHeight = lineTop - lineBot;

                if (lineHeight > 0.01f)
                {
                    float lineMidY = (lineTop + lineBot) * 0.5f;
                    columnLines[i].transform.position   = new Vector3(x, lineMidY, 0f);
                    columnLines[i].transform.localScale  =
                        new Vector3(blockSize * 0.15f, lineHeight, 1f);
                    columnLines[i].gameObject.SetActive(true);
                }
                else
                {
                    columnLines[i].gameObject.SetActive(false);
                }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void HideAll()
        {
            for (int i = 0; i < previewBlocks.Length; i++)
            {
                previewBlocks[i].gameObject.SetActive(false);
                columnLines[i].gameObject.SetActive(false);
            }
        }

        private SpriteRenderer CreateSprite(string goName, int sortingOrder)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform);
            go.SetActive(false);

            var sr         = go.AddComponent<SpriteRenderer>();
            sr.sprite       = blockSprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
