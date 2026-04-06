using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Singleton that owns level progression and grid initialization.
    /// LoadLevel / LoadNextLevel / ReloadCurrentLevel signal GameManager to enter
    /// LoadingLevel state; SceneFlowController then calls InitializeLevel() to
    /// set up the grid, droppers, and transitions to Playing.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        // ── Level data ─────────────────────────────────────────────────────────

        [Header("Level Assets — assign all 30 in order")]
        [SerializeField] private LevelData[] levels;

        // ── Gameplay refs ──────────────────────────────────────────────────────

        [Header("Gameplay References")]
        [SerializeField] private GridManager        gridManager;
        [SerializeField] private BlockDropper       blockDropper;
        [SerializeField] private GameplayController gameplayController;
        [SerializeField] private NextBlockPreview   nextBlockPreview;
        [SerializeField] private ScoreManager       scoreManager;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public navigation API ──────────────────────────────────────────────

        /// <summary>Start loading a specific level by number.</summary>
        public void LoadLevel(int levelNumber)
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.SetCurrentLevel(levelNumber);
            GameManager.Instance.ChangeState(GameState.LoadingLevel);
            Debug.Log($"[LevelManager] LoadLevel → {levelNumber}");
        }

        /// <summary>Advance to the next level (clamped at LevelCount).</summary>
        public void LoadNextLevel()
        {
            if (GameManager.Instance == null) return;
            int next = Mathf.Min(
                GameManager.Instance.CurrentLevelNumber + 1,
                PlayerProgress.LevelCount);
            GameManager.Instance.SetCurrentLevel(next);
            GameManager.Instance.ChangeState(GameState.LoadingLevel);
            Debug.Log($"[LevelManager] LoadNextLevel → {next}");
        }

        /// <summary>Restart the current level (Try Again).</summary>
        public void ReloadCurrentLevel()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.ChangeState(GameState.LoadingLevel);
            Debug.Log($"[LevelManager] ReloadCurrentLevel → {GameManager.Instance.CurrentLevelNumber}");
        }

        // ── Grid initialization — called by SceneFlowController ────────────────

        /// <summary>
        /// Finds the LevelData for <paramref name="levelNumber"/>, resets the grid,
        /// arms the dropper + controller, refreshes the preview, then signals Playing.
        ///
        /// Must be called AFTER the LoadingLevel canvas state has been applied (i.e.
        /// SceneFlowController defers this one frame via coroutine to avoid re-entrant
        /// OnStateChanged calls).
        /// </summary>
        public void InitializeLevel(int levelNumber)
        {
            LevelData data = GetLevelData(levelNumber);
            if (data == null)
            {
                Debug.LogError($"[LevelManager] No LevelData found for level {levelNumber}. " +
                               $"Make sure all 30 assets are assigned in the Inspector.");
                return;
            }

            // Reset score before the new level starts
            scoreManager?.ResetScore();

            // Initialize grid (also resets pool)
            gridManager.InitGrid(data.columnCount, data.maxHeight);
            gridManager.SpawnInitialBlocks(data.startingBlocks);

            // Arm gameplay systems
            blockDropper.SetLevel(data);
            gameplayController.SetLevel(data);
            nextBlockPreview?.Refresh();

            Debug.Log($"[LevelManager] Level {levelNumber} initialized — " +
                      $"{data.columnCount} cols, {data.dropSequence.Length} drops.");

            GameManager.Instance.ChangeState(GameState.Playing);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Returns the LevelData asset whose levelNumber matches, or null.</summary>
        public LevelData GetLevelData(int levelNumber)
        {
            if (levels == null) return null;
            foreach (var ld in levels)
                if (ld != null && ld.levelNumber == levelNumber)
                    return ld;
            return null;
        }
    }
}
