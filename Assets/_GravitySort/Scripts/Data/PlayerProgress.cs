using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Plain serializable data container for all persistent player state.
    /// Not a MonoBehaviour or ScriptableObject — purely a data bag.
    ///
    /// Usage:
    ///   var progress = PlayerProgress.Load();
    ///   progress.coins += 100;
    ///   PlayerProgress.Save(progress);
    /// </summary>
    [System.Serializable]
    public class PlayerProgress
    {
        // ── Constants ──────────────────────────────────────────────────────────

        public const int LevelCount = 30;

        private const string PrefsKey = "PlayerProgress";

        // ── Fields ─────────────────────────────────────────────────────────────

        public int highestLevelUnlocked = 1;
        public int coins                = 0;
        public int gems                 = 50;

        /// <summary>Stars earned per level (0–3). Index = levelNumber - 1.</summary>
        public int[] levelStars      = new int[LevelCount];

        /// <summary>High score per level. Index = levelNumber - 1.</summary>
        public int[] levelHighScores = new int[LevelCount];

        public bool soundEnabled    = true;
        public bool musicEnabled    = true;
        public bool hapticsEnabled  = true;

        // ── Persistence ────────────────────────────────────────────────────────

        /// <summary>
        /// Serializes <paramref name="data"/> to JSON and stores it in PlayerPrefs.
        /// </summary>
        public static void Save(PlayerProgress data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads and deserializes saved progress from PlayerPrefs.
        /// Returns a fresh default instance if no save data exists.
        /// </summary>
        public static PlayerProgress Load()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
                return CreateDefault();

            string json = PlayerPrefs.GetString(PrefsKey);
            PlayerProgress data = JsonUtility.FromJson<PlayerProgress>(json);

            // Guard against null arrays — can happen when loading save data
            // from an older version that predates these fields.
            if (data == null)
                return CreateDefault();

            if (data.levelStars      == null || data.levelStars.Length      != LevelCount)
                data.levelStars      = new int[LevelCount];

            if (data.levelHighScores == null || data.levelHighScores.Length != LevelCount)
                data.levelHighScores = new int[LevelCount];

            return data;
        }

        /// <summary>
        /// Wipes all saved progress from PlayerPrefs. Use for debug resets only.
        /// </summary>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[PlayerProgress] Save data deleted.");
        }

        // ── Convenience helpers ────────────────────────────────────────────────

        /// <summary>
        /// Returns the star count for a level (1-based index).
        /// </summary>
        public int GetStars(int levelNumber) =>
            (levelNumber >= 1 && levelNumber <= LevelCount)
                ? levelStars[levelNumber - 1]
                : 0;

        /// <summary>
        /// Sets stars for a level and records the high score if it beats the current best.
        /// </summary>
        public void RecordResult(int levelNumber, int stars, int score)
        {
            if (levelNumber < 1 || levelNumber > LevelCount) return;

            int idx = levelNumber - 1;

            if (stars > levelStars[idx])
                levelStars[idx] = stars;

            if (score > levelHighScores[idx])
                levelHighScores[idx] = score;

            if (levelNumber >= highestLevelUnlocked)
                highestLevelUnlocked = Mathf.Min(levelNumber + 1, LevelCount);
        }

        /// <summary>Returns true if the level has been unlocked.</summary>
        public bool IsUnlocked(int levelNumber) => levelNumber <= highestLevelUnlocked;

        // ── Private ────────────────────────────────────────────────────────────

        private static PlayerProgress CreateDefault() => new PlayerProgress
        {
            highestLevelUnlocked = 1,
            coins                = 0,
            gems                 = 50,
            levelStars           = new int[LevelCount],
            levelHighScores      = new int[LevelCount],
            soundEnabled         = true,
            musicEnabled         = true,
            hapticsEnabled       = true,
        };
    }
}
