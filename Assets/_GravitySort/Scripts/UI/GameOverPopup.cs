using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Popup shown when GameManager enters GameOver state.
    ///
    /// Offers three exits:
    ///   1. Gem Continue  — spends 5 gems, removes top 2 rows across all columns, resumes play.
    ///   2. Ad Continue   — same effect as gem continue but free (placeholder — just logs).
    ///   3. Try Again     — fully reloads the current level.
    ///   4. Menu          — returns to main menu.
    ///
    /// Attach to the root Canvas GameObject.
    /// </summary>
    public class GameOverPopup : MonoBehaviour
    {
        // ── Cost ───────────────────────────────────────────────────────────────
        private const int GemCost = 5;

        // ── References ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private RectTransform panel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI gemBalanceText;   // shows current gem count

        [Header("Continue Section")]
        [SerializeField] private Button gemContinueButton;
        [SerializeField] private TextMeshProUGUI gemContinueLabel; // greyed when insufficient
        [SerializeField] private Button adContinueButton;

        [Header("Primary Buttons")]
        [SerializeField] private Button tryAgainButton;
        [SerializeField] private Button menuButton;

        [Header("External — wire in Inspector")]
        [SerializeField] private ScoreManager  scoreManager;
        [SerializeField] private GridManager   gridManager;
        [SerializeField] private BlockDropper  blockDropper;
        [SerializeField] private InputHandler  inputHandler;

        [Header("Config")]
        [SerializeField] private Color buttonActiveColor   = new Color(0.20f, 0.72f, 0.35f, 1f);
        [SerializeField] private Color buttonDisabledColor = new Color(0.28f, 0.30f, 0.35f, 0.5f);
        [SerializeField] private float rollDuration        = 0.8f;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            gemContinueButton.onClick.AddListener(OnGemContinueClicked);
            adContinueButton .onClick.AddListener(OnAdContinueClicked);
            tryAgainButton   .onClick.AddListener(OnTryAgainClicked);
            menuButton       .onClick.AddListener(OnMenuClicked);
        }

        // ── Show ───────────────────────────────────────────────────────────────
        // Visibility and Show() are called by SceneFlowController.

        public void Show(int finalScore)
        {
            gameObject.SetActive(true);
            RefreshGemState();

            scoreText.text = "0";

            // Animate panel in
            panel.localScale = Vector3.zero;
            panel.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            // Roll score counter
            float displayScore = 0f;
            DOTween.To(
                () => displayScore,
                x  => { displayScore = x; scoreText.text = Mathf.RoundToInt(x).ToString("N0"); },
                finalScore,
                rollDuration
            ).SetDelay(0.35f).SetEase(Ease.OutQuad);
        }

        // ── Gem state ─────────────────────────────────────────────────────────

        private void RefreshGemState()
        {
            int gems        = GameManager.Instance?.Progress.gems ?? 0;
            bool canAfford  = gems >= GemCost;

            if (gemBalanceText != null)
                gemBalanceText.text = $"{gems} gems";

            // Update gem button appearance
            var gemImg = gemContinueButton.GetComponent<Image>();
            if (gemImg != null)
                gemImg.color = canAfford ? buttonActiveColor : buttonDisabledColor;

            gemContinueButton.interactable = canAfford;

            if (gemContinueLabel != null)
                gemContinueLabel.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        // ── Continue shared logic ──────────────────────────────────────────────

        /// <summary>
        /// Removes blocks at the top 2 rows across all columns, then resumes play.
        /// Called by both the gem path and the ad path.
        /// </summary>
        private void ExecuteContinue()
        {
            if (gridManager != null)
            {
                int topRow = gridManager.MaxRows - 1; // e.g. row 7
                for (int c = 0; c < gridManager.ColumnCount; c++)
                {
                    // Remove blocks sitting at maxHeight-1 and maxHeight-2
                    int height    = gridManager.GetColumnHeight(c);
                    int toRemove  = Mathf.Max(0, height - (gridManager.MaxRows - 2));
                    if (toRemove > 0)
                        gridManager.RemoveBlocksFromTop(c, toRemove);
                }
                gridManager.RefreshVisuals();
            }

            Hide();

            // Re-enable input and resume drops
            if (inputHandler != null) inputHandler.inputEnabled = true;
            blockDropper?.ResumeDrops();
            GameManager.Instance?.ChangeState(GameState.Playing);
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private void OnGemContinueClicked()
        {
            if (GameManager.Instance == null) return;
            int gems = GameManager.Instance.Progress.gems;
            if (gems < GemCost) return; // guard (button should be disabled anyway)

            GameManager.Instance.Progress.gems -= GemCost;
            PlayerProgress.Save(GameManager.Instance.Progress);

            Debug.Log($"[GameOverPopup] Gem continue — spent {GemCost} gems, " +
                      $"{GameManager.Instance.Progress.gems} remaining.");
            ExecuteContinue();
        }

        private void OnAdContinueClicked()
        {
            // Placeholder — real implementation will show an ad SDK interstitial.
            // When ad completes, call ExecuteContinue().
            Debug.Log("[GameOverPopup] Ad continue — ad would play here. Continuing for free.");
            ExecuteContinue();
        }

        private void OnTryAgainClicked()
        {
            blockDropper?.StopDrops();
            Hide();
            LevelManager.Instance?.ReloadCurrentLevel();
        }

        private void OnMenuClicked()
        {
            blockDropper?.StopDrops();
            Hide();
            GameManager.Instance?.ChangeState(GameState.MainMenu);
        }

        // ── Hide ───────────────────────────────────────────────────────────────

        private void Hide()
        {
            DOTween.Kill(panel);
            gameObject.SetActive(false);
        }
    }
}
