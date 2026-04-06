using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Popup shown when GameManager enters LevelComplete state.
    /// Animates in via DOScale OutBack, rolls the score counter, shows 1-3 stars,
    /// and offers Next Level / Menu buttons.
    ///
    /// Attach to the root Canvas GameObject.
    /// The Canvas starts inactive; Show() activates and animates it.
    /// </summary>
    public class LevelCompletePopup : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private RectTransform panel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI coinText;

        [Header("Stars")]
        [SerializeField] private Image[] starImages;   // exactly 3 elements

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button menuButton;

        [Header("External")]
        [SerializeField] private ScoreManager scoreManager;

        [Header("Config")]
        [SerializeField] private Color starActiveColor  = new Color(1.00f, 0.85f, 0.10f, 1f); // gold
        [SerializeField] private Color starInactiveColor = new Color(0.25f, 0.25f, 0.30f, 1f); // dark grey
        [SerializeField] private float rollDuration      = 1.0f;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            nextButton.onClick.AddListener(OnNextClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        // ── Public API ─────────────────────────────────────────────────────────
        // Visibility and Show() are called by SceneFlowController.

        public void Show(int levelNumber, int finalScore)
        {
            gameObject.SetActive(true);

            int earnedStars = CalculateStars(levelNumber, finalScore);
            int coins       = 100 + earnedStars * 50;

            SetStarVisuals(earnedStars);
            coinText.text  = $"+{coins} coins";
            scoreText.text = "0";

            // Animate panel in
            panel.localScale = Vector3.zero;
            panel.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            // Roll score counter (starts after panel lands)
            float displayScore = 0f;
            DOTween.To(
                () => displayScore,
                x  => { displayScore = x; scoreText.text = Mathf.RoundToInt(x).ToString("N0"); },
                finalScore,
                rollDuration
            ).SetDelay(0.35f).SetEase(Ease.OutQuad);
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private void OnNextClicked()
        {
            DOTween.Kill(panel);
            gameObject.SetActive(false);
            LevelManager.Instance?.LoadNextLevel();
        }

        private void OnMenuClicked()
        {
            DOTween.Kill(panel);
            gameObject.SetActive(false);
            GameManager.Instance?.ChangeState(GameState.MainMenu);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// 3 stars: score > levelNumber × 1000
        /// 2 stars: score > levelNumber × 500
        /// 1 star : completed (always)
        /// </summary>
        private static int CalculateStars(int levelNumber, int score)
        {
            if (score > levelNumber * 1000) return 3;
            if (score > levelNumber * 500)  return 2;
            return 1;
        }

        private void SetStarVisuals(int earnedStars)
        {
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].color = (i < earnedStars) ? starActiveColor : starInactiveColor;
        }
    }
}
