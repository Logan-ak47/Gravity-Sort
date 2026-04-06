using TMPro;
using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Manages HUD data display: score counter and level label.
    /// Subscribes to ScoreManager.OnScoreChanged for live score updates.
    /// SceneFlowController calls UpdateLevelLabel() when a level loads.
    /// </summary>
    public class HudManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private ScoreManager    scoreManager;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (scoreManager != null)
                scoreManager.OnScoreChanged += OnScoreChanged;

            // Initialise display values
            OnScoreChanged(0);
            UpdateLevelLabel(GameManager.Instance != null
                ? GameManager.Instance.CurrentLevelNumber
                : 1);
        }

        private void OnDestroy()
        {
            if (scoreManager != null)
                scoreManager.OnScoreChanged -= OnScoreChanged;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Called by SceneFlowController when LoadingLevel is entered.</summary>
        public void UpdateLevelLabel(int levelNumber)
        {
            if (levelText != null)
                levelText.text = $"Level {levelNumber}";
        }

        // ── Score update ───────────────────────────────────────────────────────

        private void OnScoreChanged(int newScore)
        {
            if (scoreText != null)
                scoreText.text = newScore.ToString("N0");
        }
    }
}
