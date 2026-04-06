using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Main menu screen. Shows when GameManager enters MainMenu state.
    /// PLAY button reads PlayerProgress.highestLevelUnlocked and loads that level directly.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            playButton    .onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnEnable()
        {
            RefreshPlayButton();
        }

        // ── Show / Hide ────────────────────────────────────────────────────────
        // Visibility is driven by SceneFlowController, not self-managed.

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshPlayButton();
        }

        public void Hide() => gameObject.SetActive(false);

        // ── Helpers ────────────────────────────────────────────────────────────

        private void RefreshPlayButton()
        {
            var progress = GameManager.Instance?.Progress ?? PlayerProgress.Load();
            int level    = Mathf.Max(1, progress.highestLevelUnlocked);
            var label    = playButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"PLAY LEVEL {level}";
        }

        // ── Button handlers ────────────────────────────────────────────────────

        private void OnPlayClicked()
        {
            var progress = GameManager.Instance?.Progress ?? PlayerProgress.Load();
            int level    = Mathf.Max(1, progress.highestLevelUnlocked);
            LevelManager.Instance?.LoadLevel(level);
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings tapped — not yet implemented.");
        }
    }
}
