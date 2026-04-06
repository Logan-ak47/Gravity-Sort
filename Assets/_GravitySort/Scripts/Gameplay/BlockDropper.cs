using UnityEngine;

namespace GravitySort
{
    public class BlockDropper : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager  gridManager;
        [SerializeField] private GameConfig   config;
        [SerializeField] private InputHandler inputHandler;

        // Assigned at level load time via SetLevel()
        private LevelData levelData;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired when a drop targets a full column. Carries column index.</summary>
        public event System.Action<int> OnColumnOverflow;

        /// <summary>
        /// Fired immediately after a block is dispatched (index incremented, animation starting).
        /// Used by NextBlockPreview so it shows what's truly next while the block is in flight.
        /// </summary>
        public event System.Action OnDropDispatched;

        /// <summary>
        /// Fired after a drop animation lands, carrying the destination column.
        /// Subscriber (GameplayController) should call StartChainCheck here.
        /// Input is still locked — ChainReactionHandler.OnChainComplete re-enables it.
        /// </summary>
        public event System.Action<int> OnBlockDropped;

        /// <summary>
        /// Fired once when the full dropSequence has been consumed.
        /// No new blocks will fall; player clears what remains.
        /// </summary>
        public event System.Action OnAllDropsComplete;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Master on/off switch. Toggle with PauseDrops/ResumeDrops/StopDrops.
        /// Animation pauses are handled automatically via InputHandler.inputEnabled.
        /// </summary>
        public bool isActive;

        private int  currentDropIndex;
        private float dropTimer;
        private bool  allDropsFired;   // guards OnAllDropsComplete to fire only once

        // ── API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns the level and begins the drop countdown.
        /// Call once after InitGrid/SpawnInitialBlocks when a level starts.
        /// </summary>
        public void SetLevel(LevelData data)
        {
            levelData        = data;
            currentDropIndex = 0;
            allDropsFired    = false;
            dropTimer        = levelData.dropInterval;
            isActive         = true;
        }

        /// <summary>Pauses the drop timer (Freeze booster, cutscenes, etc.).</summary>
        public void PauseDrops()  => isActive = false;

        /// <summary>Resumes the drop timer after an intentional pause.</summary>
        public void ResumeDrops() => isActive = true;

        /// <summary>Permanently stops drops (level complete or game over).</summary>
        public void StopDrops()
        {
            isActive         = false;
            currentDropIndex = levelData != null ? levelData.dropSequence.Length : 0;
        }

        /// <summary>
        /// Returns the next <paramref name="count"/> DropEntry items without
        /// advancing the drop index. Used by the preview UI.
        /// Returns fewer entries if fewer remain in the sequence.
        /// </summary>
        public DropEntry[] GetUpcomingDrops(int count)
        {
            if (levelData?.dropSequence == null)
                return System.Array.Empty<DropEntry>();

            int available = Mathf.Min(count,
                levelData.dropSequence.Length - currentDropIndex);

            if (available <= 0)
                return System.Array.Empty<DropEntry>();

            var result = new DropEntry[available];
            System.Array.Copy(levelData.dropSequence, currentDropIndex,
                              result, 0, available);
            return result;
        }

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!isActive)                  return; // externally paused / stopped
            if (levelData == null)          return;
            if (!inputHandler.inputEnabled) return; // pour / chain / drop in progress

            dropTimer -= Time.deltaTime;
            if (dropTimer <= 0f)
            {
                dropTimer = levelData.dropInterval; // reset before drop, not after
                ExecuteDrop();
            }
        }

        // ── Drop execution ─────────────────────────────────────────────────────

        private void ExecuteDrop()
        {
            // Sequence exhausted — fire the "no more drops" event once
            if (currentDropIndex >= levelData.dropSequence.Length)
            {
                if (!allDropsFired)
                {
                    allDropsFired = true;
                    isActive      = false;
                    OnAllDropsComplete?.Invoke();
                }
                return;
            }

            DropEntry entry = levelData.dropSequence[currentDropIndex];
            currentDropIndex++;

            // Notify preview immediately — index already advanced, so preview
            // shows what comes AFTER the block now in flight.
            OnDropDispatched?.Invoke();

            // Fire OnAllDropsComplete immediately after dispatching the last block
            // so allDropsExhausted is true before that drop's chain check resolves.
            if (!allDropsFired && currentDropIndex >= levelData.dropSequence.Length)
            {
                allDropsFired = true;
                isActive      = false;
                OnAllDropsComplete?.Invoke();
            }

            // Column already at capacity — signal overflow instead of dropping
            if (gridManager.IsColumnFull(entry.column))
            {
                OnColumnOverflow?.Invoke(entry.column);
                return;
            }

            // Lock input for the duration of the animation + chain reaction.
            // ChainReactionHandler.OnChainComplete (subscribed in GameplayController)
            // re-enables input once the full chain resolves.
            inputHandler.inputEnabled = false;

            // Add block to data + spawn visual at its final grid position
            gridManager.AddBlockToColumn(entry.column, entry.colorIndex);

            int     newRow = gridManager.GetColumnHeight(entry.column) - 1;
            Block   block  = gridManager.GetBlockVisual(entry.column, newRow);
            Vector3 target = block.transform.position;

            // Spawn 2 cell-heights above the top row so it enters from off-screen
            block.transform.position =
                gridManager.GetWorldPosition(entry.column, gridManager.MaxRows + 2);

            int capturedCol = entry.column;
            block.PlayDropAnimation(target, config.dropDuration, () =>
            {
                // Fire dropped event — GameplayController calls StartChainCheck,
                // which keeps input locked until the full chain resolves.
                OnBlockDropped?.Invoke(capturedCol);
            });
        }
    }
}
