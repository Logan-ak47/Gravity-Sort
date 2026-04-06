using System.Collections;
using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Single source of truth for canvas visibility.
    /// Subscribes to GameManager.OnStateChanged and toggles the correct canvases.
    ///
    /// Canvas sort-order reference:
    ///   MainMenuCanvas     sortOrder = 1
    ///   HudCanvas          sortOrder = 2
    ///   LevelSelectCanvas  sortOrder = 5   (managed by MainMenu PLAY button)
    ///   GameOverCanvas     sortOrder = 10
    ///   LevelCompleteCanvas sortOrder = 10
    ///
    /// LevelSelectCanvas is NOT managed here — it is shown/hidden by MainMenu's
    /// PLAY button and LevelSelectUI's Back button, both inside the MainMenu state.
    /// </summary>
    public class SceneFlowController : MonoBehaviour
    {
        // ── Canvas GameObjects ──────────────────────────────────────────────────

        [Header("Canvases")]
        [SerializeField] private GameObject mainMenuCanvas;
        [SerializeField] private GameObject hudCanvas;
        [SerializeField] private GameObject levelCompleteCanvas;
        [SerializeField] private GameObject gameOverCanvas;

        // ── Popup components (called to pass data on show) ──────────────────────

        [Header("Popup Components")]
        [SerializeField] private LevelCompletePopup levelCompletePopup;
        [SerializeField] private GameOverPopup      gameOverPopup;

        // ── Gameplay references ─────────────────────────────────────────────────

        [Header("Gameplay")]
        [SerializeField] private LevelManager  levelManager;
        [SerializeField] private ScoreManager  scoreManager;
        [SerializeField] private HudManager    hudManager;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[SceneFlowController] GameManager not found.");
                return;
            }

            GameManager.Instance.OnStateChanged += HandleStateChanged;

            // GameManager.RunBoot fires before any Start(). Apply current state now.
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        // ── State handler ─────────────────────────────────────────────────────

        private void HandleStateChanged(GameState newState)
        {
            // Reset all to hidden first, then show what's needed.
            // LevelSelectCanvas is intentionally excluded — owned by MainMenu.
            SetActive(mainMenuCanvas,      false);
            SetActive(hudCanvas,           false);
            SetActive(levelCompleteCanvas, false);
            SetActive(gameOverCanvas,      false);

            switch (newState)
            {
                case GameState.Boot:
                    // Nothing to show; Boot completes in the same frame as Awake.
                    break;

                case GameState.MainMenu:
                    SetActive(mainMenuCanvas, true);
                    break;

                case GameState.LoadingLevel:
                    SetActive(hudCanvas, true);
                    hudManager?.UpdateLevelLabel(GameManager.Instance.CurrentLevelNumber);
                    // Defer grid init by one frame so this OnStateChanged call unwinds first.
                    StartCoroutine(InitLevelNextFrame());
                    break;

                case GameState.Playing:
                case GameState.Pouring:
                case GameState.ChainCheck:
                case GameState.Clearing:
                    SetActive(hudCanvas, true);
                    break;

                case GameState.Paused:
                    SetActive(hudCanvas, true);
                    // Phase 6: show pause panel here.
                    break;

                case GameState.LevelComplete:
                    SetActive(hudCanvas,           true);
                    SetActive(levelCompleteCanvas,  true);
                    int lvl   = GameManager.Instance.CurrentLevelNumber;
                    int wscore = scoreManager != null ? scoreManager.CurrentScore : 0;
                    levelCompletePopup?.Show(lvl, wscore);
                    break;

                case GameState.GameOver:
                    SetActive(hudCanvas,      true);
                    SetActive(gameOverCanvas,  true);
                    int lscore = scoreManager != null ? scoreManager.CurrentScore : 0;
                    gameOverPopup?.Show(lscore);
                    break;
            }
        }

        // ── Level init ─────────────────────────────────────────────────────────

        /// <summary>
        /// Defers InitializeLevel by one frame so HandleStateChanged(LoadingLevel)
        /// fully unwinds before InitializeLevel calls ChangeState(Playing).
        /// </summary>
        private IEnumerator InitLevelNextFrame()
        {
            yield return null;
            levelManager?.InitializeLevel(GameManager.Instance.CurrentLevelNumber);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
