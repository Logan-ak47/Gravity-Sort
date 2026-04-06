using UnityEngine;

namespace GravitySort
{
    public class GameplayController : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager          gridManager;
        [SerializeField] private InputHandler         inputHandler;
        [SerializeField] private PourAnimator         pourAnimator;
        [SerializeField] private ChainReactionHandler chainReactionHandler;
        [SerializeField] private BlockDropper         blockDropper;

        // Assigned at level load time via SetLevel()
        private LevelData levelData;

        // ── State ──────────────────────────────────────────────────────────────

        private int  selectedColumn    = -1;
        private bool allDropsExhausted = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            inputHandler.OnColumnTapped          += HandleColumnTapped;
            chainReactionHandler.OnChainComplete += HandleChainComplete;
            chainReactionHandler.OnBlocksCleared += HandleBlocksCleared;
            blockDropper.OnBlockDropped          += HandleBlockDropped;
            blockDropper.OnColumnOverflow        += HandleColumnOverflow;
            blockDropper.OnAllDropsComplete      += HandleAllDropsComplete;
        }

        private void OnDisable()
        {
            inputHandler.OnColumnTapped          -= HandleColumnTapped;
            chainReactionHandler.OnChainComplete -= HandleChainComplete;
            chainReactionHandler.OnBlocksCleared -= HandleBlocksCleared;
            blockDropper.OnBlockDropped          -= HandleBlockDropped;
            blockDropper.OnColumnOverflow        -= HandleColumnOverflow;
            blockDropper.OnAllDropsComplete      -= HandleAllDropsComplete;
        }

        // ── Input handling ─────────────────────────────────────────────────────

        private void HandleColumnTapped(int column)
        {
            // Only process taps when the game is in an interactive state.
            // InputHandler.inputEnabled is the low-level animation gate;
            // GameManager.CurrentState is the semantic gate.
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            // Nothing selected yet — select tapped column if non-empty
            if (selectedColumn == -1)
            {
                if (gridManager.IsColumnEmpty(column)) return;
                Select(column);
                return;
            }

            // Same column tapped — deselect
            if (column == selectedColumn)
            {
                Deselect();
                return;
            }

            // Different column tapped — attempt pour
            if (!CanPour(selectedColumn, column))
            {
                Deselect();
                return;
            }

            StartPour(selectedColumn, column);
        }

        // ── Pour validation ────────────────────────────────────────────────────

        public bool CanPour(int sourceCol, int destCol)
        {
            if (sourceCol == destCol)                       return false;
            if (gridManager.IsColumnEmpty(sourceCol))       return false;
            if (gridManager.IsColumnFull(destCol))          return false;

            // Dest must be empty OR share the same top color as source
            if (!gridManager.IsColumnEmpty(destCol) &&
                gridManager.GetTopColor(destCol) != gridManager.GetTopColor(sourceCol))
                return false;

            return true;
        }

        // ── Pour flow ──────────────────────────────────────────────────────────

        private void StartPour(int sourceCol, int destCol)
        {
            // Lock input (animation gate) and signal semantic state
            inputHandler.inputEnabled = false;
            blockDropper.PauseDrops();
            GameManager.Instance?.ChangeState(GameState.Pouring);

            // Grab block visual refs BEFORE data is modified
            Block[] topGroupBlocks = gridManager.GetTopColorGroupBlocks(sourceCol);

            // Clear selection visuals immediately
            foreach (Block b in topGroupBlocks)
                b.SetSelected(false);
            selectedColumn = -1;

            // Modify data; determine how many blocks actually move
            int moveCount = ExecutePour(sourceCol, destCol);

            // Build the exact sub-array the animator needs ([0] = topmost)
            Block[] blocksToAnimate = new Block[moveCount];
            System.Array.Copy(topGroupBlocks, blocksToAnimate, moveCount);

            pourAnimator.AnimatePour(blocksToAnimate, moveCount, destCol, () =>
            {
                SettleColumn(sourceCol, () =>
                {
                    // Pour + settle complete — start checking for matches
                    GameManager.Instance?.ChangeState(GameState.ChainCheck);
                    chainReactionHandler.StartChainCheck();
                });
            });
        }

        // ── Pour execution (data only) ─────────────────────────────────────────

        // Transfers colorIndex values between columnData arrays.
        // Clears source blockVisuals slots without deactivating blocks (animator owns them).
        // Pre-populates dest columnData entries so PourAnimator can compute landing rows.
        // Returns the number of blocks moved.
        private int ExecutePour(int sourceCol, int destCol)
        {
            var (colorIndex, groupCount) = gridManager.GetTopColorGroup(sourceCol);

            int available = gridManager.MaxRows - gridManager.GetColumnHeight(destCol);
            int moveCount = Mathf.Min(groupCount, available);

            gridManager.RemoveBlocksFromTopDataOnly(sourceCol, moveCount);
            for (int i = 0; i < moveCount; i++)
                gridManager.AddBlockToColumnData(destCol, colorIndex);

            return moveCount;
        }

        // ── Settle ─────────────────────────────────────────────────────────────

        private void SettleColumn(int col, System.Action onComplete)
        {
            gridManager.SettleColumn(col, onComplete);
        }

        // ── Chain reaction callbacks ───────────────────────────────────────────

        /// <summary>
        /// Single exit point for all animation sequences (pour-chain and drop-chain).
        /// Re-enables input and resumes drops together so they're always in sync.
        /// CheckWinCondition may change state to LevelComplete before this returns.
        /// </summary>
        private void HandleChainComplete(int comboCount)
        {
            inputHandler.inputEnabled = true;
            blockDropper.ResumeDrops();
            CheckWinCondition();

            // Return to Playing unless CheckWinCondition already ended the level
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState != GameState.LevelComplete &&
                GameManager.Instance.CurrentState != GameState.GameOver)
            {
                GameManager.Instance.ChangeState(GameState.Playing);
            }
        }

        private void HandleBlocksCleared(int blocksCleared, int comboStep)
        {
            // Blocks are being animated out — signal the clearing state.
            // ChainReactionHandler will loop back to ChainCheck internally after settle.
            GameManager.Instance?.ChangeState(GameState.Clearing);
        }

        // ── BlockDropper callbacks ─────────────────────────────────────────────

        /// <summary>
        /// A dropped block has landed. Input is still locked (ExecuteDrop holds it).
        /// Kick off a chain check — HandleChainComplete re-enables everything when done.
        /// </summary>
        private void HandleBlockDropped(int column)
        {
            GameManager.Instance?.ChangeState(GameState.ChainCheck);
            chainReactionHandler.StartChainCheck();
        }

        /// <summary>
        /// A block was scheduled to drop into an already-full column — game over.
        /// </summary>
        private void HandleColumnOverflow(int column)
        {
            blockDropper.PauseDrops(); // PauseDrops (not StopDrops) so gem/ad continue can ResumeDrops
            inputHandler.inputEnabled = false;
            GameManager.Instance?.ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// The full drop sequence has been consumed.
        /// Keep gameplay alive; win check happens after each subsequent chain completes.
        /// </summary>
        private void HandleAllDropsComplete()
        {
            allDropsExhausted = true;
            Debug.Log("[GameplayController] All drops exhausted — player clears what remains.");
        }

        // ── Level API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Call this (alongside BlockDropper.SetLevel) whenever a level starts.
        /// Resets per-level state so win detection is clean on replay.
        /// </summary>
        public void SetLevel(LevelData data)
        {
            levelData                 = data;
            allDropsExhausted         = (data.dropSequence == null || data.dropSequence.Length == 0);
            selectedColumn            = -1;
            inputHandler.inputEnabled = true;
        }

        // ── Win condition ──────────────────────────────────────────────────────

        private void CheckWinCondition()
        {
            if (levelData == null) return;

            bool won = false;

            switch (levelData.winCondition)
            {
                case WinConditionType.ClearAll:
                    // All drops must have landed before we can declare a ClearAll win.
                    if (!allDropsExhausted) return;

                    won = true;
                    for (int c = 0; c < gridManager.ColumnCount; c++)
                    {
                        if (!gridManager.IsColumnEmpty(c)) { won = false; break; }
                    }
                    break;

                case WinConditionType.ReduceBelow:
                    int total = 0;
                    for (int c = 0; c < gridManager.ColumnCount; c++)
                        total += gridManager.GetColumnHeight(c);
                    won = total < levelData.winTarget;
                    break;

                case WinConditionType.Survive:
                    return; // time-based, not evaluated here
            }

            if (!won) return;

            blockDropper.StopDrops();
            inputHandler.inputEnabled = false;
            GameManager.Instance?.ChangeState(GameState.LevelComplete);
        }

        // ── Selection helpers ──────────────────────────────────────────────────

        private void Select(int column)
        {
            selectedColumn = column;
            foreach (Block b in gridManager.GetTopColorGroupBlocks(column))
                b.SetSelected(true);
        }

        private void Deselect()
        {
            if (selectedColumn == -1) return;
            foreach (Block b in gridManager.GetTopColorGroupBlocks(selectedColumn))
                b.SetSelected(false);
            selectedColumn = -1;
        }
    }
}
