using UnityEngine;

namespace GravitySort
{
    public class ScoreManager : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private ChainReactionHandler chainReactionHandler;

        // ── State ──────────────────────────────────────────────────────────────

        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired whenever the score changes, carrying the new total.</summary>
        public event System.Action<int> OnScoreChanged;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Resets score and combo to zero. Call when a level (re)loads.</summary>
        public void ResetScore()
        {
            CurrentScore = 0;
            CurrentCombo = 0;
            OnScoreChanged?.Invoke(0);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            chainReactionHandler.OnBlocksCleared += HandleBlocksCleared;
            chainReactionHandler.OnChainComplete += HandleChainComplete;
        }

        private void OnDisable()
        {
            chainReactionHandler.OnBlocksCleared -= HandleBlocksCleared;
            chainReactionHandler.OnChainComplete -= HandleChainComplete;
        }

        // ── Scoring ────────────────────────────────────────────────────────────

        private void HandleBlocksCleared(int blocksCleared, int comboStep)
        {
            CurrentCombo++;

            int basePoints  = BasePoints(blocksCleared);
            int multiplier  = ComboMultiplier(comboStep);
            int points      = basePoints * multiplier;

            CurrentScore += points;
            OnScoreChanged?.Invoke(CurrentScore);

            Debug.Log($"[Score] +{points} (base {basePoints} × {multiplier}x combo) " +
                      $"| cleared {blocksCleared} blocks | combo step {comboStep} " +
                      $"| total {CurrentScore}");
        }

        private void HandleChainComplete(int finalCombo)
        {
            if (finalCombo > 0)
                Debug.Log($"[Score] Chain complete — {finalCombo} step(s) | total {CurrentScore}");

            CurrentCombo = 0;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Base point value for a single clear step by block count.</summary>
        private static int BasePoints(int blocksCleared)
        {
            if (blocksCleared >= 5) return 350;
            if (blocksCleared == 4) return 200;
            return 100; // 3 or fewer
        }

        /// <summary>Multiplier by combo step (1-indexed).</summary>
        private static int ComboMultiplier(int comboStep)
        {
            if (comboStep >= 4) return 5;
            if (comboStep == 3) return 3;
            if (comboStep == 2) return 2;
            return 1;
        }
    }
}
