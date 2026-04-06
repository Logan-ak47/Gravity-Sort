using UnityEditor;
using UnityEngine;

namespace GravitySort.Editor
{
    public static class GameConfigCreator
    {
        private const string AssetPath = "Assets/_GravitySort/ScriptableObjects/GameConfig.asset";

        [MenuItem("GravitySort/Create GameConfig Asset")]
        public static void CreateGameConfigAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPath) != null)
            {
                Debug.LogWarning($"GameConfig asset already exists at {AssetPath}");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPath));
                return;
            }

            var config = ScriptableObject.CreateInstance<GameConfig>();

            // Block colors — exact values from GDD section 6.3
            config.blockColors = new Color[]
            {
                new Color(1f, 0.3f, 0.42f),     // Index 0 — Red    #FF4D6A
                new Color(0.3f, 0.65f, 1f),     // Index 1 — Blue   #4DA6FF
                new Color(0.36f, 0.86f, 0.43f), // Index 2 — Green  #5BDB6E
                new Color(1f, 0.85f, 0.3f),     // Index 3 — Yellow #FFD84D
                new Color(0.7f, 0.3f, 1f)       // Index 4 — Purple #B44DFF
            };

            // Grid
            config.gridPaddingPercent = 0.05f;
            config.blockSpacing = 0.05f;
            config.gridBottomOffset = -2f;

            // Animation timing
            config.pourArcHeight = 1.5f;
            config.pourBlockDuration = 0.2f;
            config.pourStagger = 0.08f;
            config.dropDuration = 0.4f;
            config.clearDuration = 0.3f;
            config.settleDuration = 0.2f;
            config.selectionPulseScale = 1.08f;
            config.selectionPulseDuration = 0.4f;

            // Gameplay
            config.matchThreshold = 3;
            config.previewCount = 3;

            // Boosters
            config.freeUndo = 2;
            config.freeFreeze = 1;
            config.freeBomb = 1;
            config.freezeDuration = 10f;

            // Economy
            config.undoGemCost = 5;
            config.freezeGemCost = 8;
            config.bombGemCost = 10;
            config.continueGemCost = 5;

            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"GameConfig asset created at {AssetPath}");
        }
    }
}
