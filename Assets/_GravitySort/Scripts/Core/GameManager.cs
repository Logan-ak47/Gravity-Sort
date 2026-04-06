using UnityEngine;

namespace GravitySort
{
    public enum GameState
    {
        Boot,
        MainMenu,
        LoadingLevel,
        Playing,
        Pouring,
        Clearing,
        ChainCheck,
        LevelComplete,
        GameOver,
        Paused
    }

    /// <summary>
    /// Singleton game state machine. Survives scene loads via DontDestroyOnLoad.
    /// Owns PlayerProgress persistence (via PlayerPrefs until PlayerProgress.cs is built).
    ///
    /// Does NOT hold references to scene-specific components (GridManager, BlockDropper, etc.)
    /// because those are destroyed on scene transitions. Scene systems call ChangeState() to
    /// signal transitions; they subscribe to OnStateChanged to react to them.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GameConfig config;

        // ── State ──────────────────────────────────────────────────────────────

        public GameState CurrentState { get; private set; }

        /// <summary>Fired after every state change. Passes the new state.</summary>
        public event System.Action<GameState> OnStateChanged;

        // ── Level tracking ─────────────────────────────────────────────────────

        public int CurrentLevelNumber { get; private set; }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Run boot sequence synchronously in Awake so that all Start() calls
            // in the same scene see a valid CurrentState from the beginning.
            RunBoot();
        }

        // ── Boot ───────────────────────────────────────────────────────────────

        private void RunBoot()
        {
            // Set Boot state directly (no prior state to validate against)
            CurrentState = GameState.Boot;
            OnStateChanged?.Invoke(GameState.Boot);

            LoadPlayerProgress();

            // Transition to MainMenu — Phase 4 will activate the menu panel/scene here.
            // During development, TestBootstrap drives the flow from here.
            ForceState(GameState.MainMenu);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Request a state transition. Logs a warning for transitions that aren't in the
        /// allowed table but still applies the change — keeps dev iteration friction-free.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (!IsValidTransition(CurrentState, newState))
            {
                Debug.LogWarning(
                    $"[GameManager] Unexpected transition {CurrentState} → {newState}");
            }

            ApplyState(newState);
        }

        /// <summary>Set which level is currently being played (call before ChangeState(LoadingLevel)).</summary>
        public void SetCurrentLevel(int levelNumber)
        {
            CurrentLevelNumber = levelNumber;
        }

        // ── Internal state application ─────────────────────────────────────────

        /// <summary>Applies a state change without transition validation (Boot sequence).</summary>
        private void ForceState(GameState newState)
        {
            ApplyState(newState);
        }

        private void ApplyState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            OnEnterState(newState);
        }

        private void OnEnterState(GameState state)
        {
            switch (state)
            {
                case GameState.LevelComplete:
                    Debug.Log($"[GameManager] LEVEL COMPLETE — Level {CurrentLevelNumber}");
                    SavePlayerProgress();
                    break;

                case GameState.GameOver:
                    Debug.Log($"[GameManager] GAME OVER — Level {CurrentLevelNumber}");
                    break;

                // Future states (MainMenu, LoadingLevel) will trigger scene/panel loading here.
            }
        }

        // ── Transition validation ──────────────────────────────────────────────

        private static bool IsValidTransition(GameState from, GameState to)
        {
            return (from, to) switch
            {
                // ── Boot sequence ──────────────────────────────────────────────
                (GameState.Boot,          GameState.MainMenu)       => true,
                (GameState.MainMenu,      GameState.LoadingLevel)   => true,
                (GameState.LoadingLevel,  GameState.Playing)        => true,

                // ── Core gameplay loop ─────────────────────────────────────────
                (GameState.Playing,       GameState.Pouring)        => true,
                (GameState.Playing,       GameState.ChainCheck)     => true,   // drop-triggered
                (GameState.Playing,       GameState.LevelComplete)  => true,
                (GameState.Playing,       GameState.GameOver)       => true,
                (GameState.Playing,       GameState.Paused)         => true,

                // ── Animation states ───────────────────────────────────────────
                (GameState.Pouring,       GameState.ChainCheck)     => true,
                (GameState.ChainCheck,    GameState.Clearing)       => true,
                (GameState.ChainCheck,    GameState.Playing)        => true,   // no matches
                (GameState.ChainCheck,    GameState.LevelComplete)  => true,
                (GameState.ChainCheck,    GameState.GameOver)       => true,
                (GameState.Clearing,      GameState.ChainCheck)     => true,   // loop

                // ── End states ─────────────────────────────────────────────────
                (GameState.LevelComplete, GameState.LoadingLevel)   => true,
                (GameState.LevelComplete, GameState.MainMenu)       => true,
                (GameState.GameOver,      GameState.LoadingLevel)   => true,
                (GameState.GameOver,      GameState.MainMenu)       => true,
                (GameState.GameOver,      GameState.Playing)        => true,   // continue (gem / ad)

                // ── Pause ──────────────────────────────────────────────────────
                (GameState.Paused,        GameState.Playing)        => true,

                // ── Dev convenience: LoadingLevel is reachable from anywhere ───
                (_,                       GameState.LoadingLevel)   => true,

                _ => false
            };
        }

        // ── Player Progress ────────────────────────────────────────────────────
        //
        // PlayerProgress.cs is not yet built. We store minimal progress in PlayerPrefs
        // as a placeholder. When PlayerProgress.cs is implemented, migrate these reads
        // and writes into that class.

        /// <summary>
        /// Live progress for the current session. Populated in Boot, persisted on
        /// LevelComplete. Other systems read this via GameManager.Instance.Progress.
        /// </summary>
        public PlayerProgress Progress { get; private set; }

        private void LoadPlayerProgress()
        {
            Progress           = PlayerProgress.Load();
            CurrentLevelNumber = Progress.highestLevelUnlocked;
            Debug.Log($"[GameManager] Boot — last level: {CurrentLevelNumber}, " +
                      $"highest unlocked: {Progress.highestLevelUnlocked}, " +
                      $"gems: {Progress.gems}");
        }

        private void SavePlayerProgress()
        {
            // Records minimal result (0 stars, score 0) so unlock state advances.
            // Phase 5 will wire real star rating + score from ScoreManager here.
            Progress.RecordResult(CurrentLevelNumber, stars: 0, score: 0);
            PlayerProgress.Save(Progress);
        }
    }
}
