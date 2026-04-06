using UnityEditor;
using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Editor tool: GravitySort → Wire Scene References
    /// Finds every manager and canvas in the scene and sets all SerializeField
    /// references that would otherwise require manual drag-and-drop.
    /// Run this once after the scene is fully set up.
    /// </summary>
    public static class SceneWirer
    {
        [MenuItem("GravitySort/Wire Scene References")]
        public static void WireAll()
        {
            int wired = 0;

            // ── Locate scene objects ────────────────────────────────────────────
            var sceneFlow          = FindComp<SceneFlowController>("SceneFlow");
            var levelManager       = FindComp<LevelManager>("LevelManager");
            var hudManager         = FindComp<HudManager>("HudCanvas");
            var levelCompletePopup = FindComp<LevelCompletePopup>("LevelCompleteCanvas");
            var gameOverPopup      = FindComp<GameOverPopup>("GameOverCanvas");

            var managerGO     = FindGO("Manager");
            var gridManagerGO = FindGO("GridManager");

            var scoreManager     = managerGO?.GetComponent<ScoreManager>();
            var blockDropper     = managerGO?.GetComponent<BlockDropper>();
            var gameplayCtrl     = managerGO?.GetComponent<GameplayController>();
            var inputHandler     = managerGO?.GetComponent<InputHandler>();
            var nextBlockPreview = managerGO?.GetComponent<NextBlockPreview>();
            var gridManager      = gridManagerGO?.GetComponent<GridManager>();

            var mainMenuCanvas      = FindGO("MainMenuCanvas");
            var hudCanvas           = FindGO("HudCanvas");
            var levelCompleteCanvas = FindGO("LevelCompleteCanvas");
            var gameOverCanvas      = FindGO("GameOverCanvas");

            // ── SceneFlowController ────────────────────────────────────────────
            if (sceneFlow != null)
            {
                var so = new SerializedObject(sceneFlow);
                wired += SetRef(so, "mainMenuCanvas",      mainMenuCanvas);
                wired += SetRef(so, "hudCanvas",           hudCanvas);
                wired += SetRef(so, "levelCompleteCanvas", levelCompleteCanvas);
                wired += SetRef(so, "gameOverCanvas",      gameOverCanvas);
                wired += SetRef(so, "levelCompletePopup",  levelCompletePopup);
                wired += SetRef(so, "gameOverPopup",       gameOverPopup);
                wired += SetRef(so, "levelManager",        levelManager);
                wired += SetRef(so, "scoreManager",        scoreManager);
                wired += SetRef(so, "hudManager",          hudManager);
                so.ApplyModifiedProperties();
            }
            else Debug.LogWarning("[SceneWirer] SceneFlowController not found on 'SceneFlow' GO.");

            // ── LevelManager ───────────────────────────────────────────────────
            if (levelManager != null)
            {
                var so = new SerializedObject(levelManager);
                wired += SetRef(so, "gridManager",        gridManager);
                wired += SetRef(so, "blockDropper",       blockDropper);
                wired += SetRef(so, "gameplayController", gameplayCtrl);
                wired += SetRef(so, "nextBlockPreview",   nextBlockPreview);
                wired += SetRef(so, "scoreManager",       scoreManager);
                so.ApplyModifiedProperties();
            }
            else Debug.LogWarning("[SceneWirer] LevelManager not found.");

            // ── HudManager ─────────────────────────────────────────────────────
            if (hudManager != null)
            {
                var so = new SerializedObject(hudManager);
                wired += SetRef(so, "scoreManager", scoreManager);
                so.ApplyModifiedProperties();
            }

            // ── NextBlockPreview ───────────────────────────────────────────────
            if (nextBlockPreview != null)
            {
                var config  = AssetDatabase.LoadAssetAtPath<GameConfig>(
                    "Assets/_GravitySort/ScriptableObjects/GameConfig.asset");
                var sprite  = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_GravitySort/Sprites/block_white.png");

                var so = new SerializedObject(nextBlockPreview);
                wired += SetRef(so, "blockDropper", blockDropper);
                wired += SetRef(so, "gridManager",  gridManager);
                wired += SetRef(so, "config",        config);
                wired += SetRef(so, "blockSprite",   sprite);
                so.ApplyModifiedProperties();
            }

            // ── LevelCompletePopup ─────────────────────────────────────────────
            if (levelCompletePopup != null)
            {
                var so = new SerializedObject(levelCompletePopup);
                wired += SetRef(so, "scoreManager", scoreManager);
                so.ApplyModifiedProperties();
            }

            // ── GameOverPopup ─────────────────────────────────────────────────
            if (gameOverPopup != null)
            {
                var so = new SerializedObject(gameOverPopup);
                wired += SetRef(so, "scoreManager",  scoreManager);
                wired += SetRef(so, "gridManager",   gridManager);
                wired += SetRef(so, "blockDropper",  blockDropper);
                wired += SetRef(so, "inputHandler",  inputHandler);
                so.ApplyModifiedProperties();
            }

            // ── Mark scene dirty ───────────────────────────────────────────────
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[SceneWirer] Done — {wired} reference(s) wired. " +
                      "Assign the 30 Level assets to LevelManager.levels[] manually.");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Finds a root GameObject by name including inactive ones.</summary>
        private static GameObject FindGO(string name)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.scene.isLoaded && go.name == name && go.transform.parent == null)
                    return go;
            return null;
        }

        private static T FindComp<T>(string goName) where T : Component
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            foreach (var c in all)
                if (c.gameObject.scene.isLoaded && c.gameObject.name == goName)
                    return c;
            return null;
        }

        private static int SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneWirer] Property '{fieldName}' not found on {so.targetObject.GetType().Name}");
                return 0;
            }
            if (value == null)
            {
                Debug.LogWarning($"[SceneWirer] Value for '{fieldName}' is null — skipping.");
                return 0;
            }
            prop.objectReferenceValue = value;
            return 1;
        }
    }
}
