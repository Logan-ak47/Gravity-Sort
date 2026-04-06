using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Editor tool: GravitySort → Create HUD UI
    /// Builds a minimal top-bar HUD canvas with level label and score display.
    /// </summary>
    public static class HudUIBuilder
    {
        private static readonly Color BarColor  = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color LabelColor = new Color(0.55f, 0.60f, 0.72f, 1f);

        [MenuItem("GravitySort/Create HUD UI")]
        public static void Build()
        {
            var existing = GameObject.Find("HudCanvas");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[HudUIBuilder] Removed existing HudCanvas.");
            }

            // ── Canvas ─────────────────────────────────────────────────────────
            var canvasGO = new GameObject("HudCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUD UI");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2; // above MainMenu (1), below popups (10)

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Top bar ────────────────────────────────────────────────────────
            var barRt = MakeRect("TopBar", canvasGO.transform);
            barRt.anchorMin        = new Vector2(0f, 1f);
            barRt.anchorMax        = new Vector2(1f, 1f);
            barRt.pivot            = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta        = new Vector2(0f, 130f);
            barRt.gameObject.AddComponent<Image>().color = BarColor;

            // Level label — left side
            var levelRt  = MakeRect("LevelText", barRt);
            levelRt.anchorMin        = new Vector2(0f, 0f);
            levelRt.anchorMax        = new Vector2(0.5f, 1f);
            levelRt.offsetMin        = new Vector2(32f, 0f);
            levelRt.offsetMax        = new Vector2(0f, -10f);
            var levelTmp = levelRt.gameObject.AddComponent<TextMeshProUGUI>();
            levelTmp.text               = "Level 1";
            levelTmp.fontSize           = 36;
            levelTmp.color              = LabelColor;
            levelTmp.alignment          = TextAlignmentOptions.MidlineLeft;
            levelTmp.enableWordWrapping = false;
            levelTmp.raycastTarget      = false;

            // Score — right side
            var scoreRt  = MakeRect("ScoreText", barRt);
            scoreRt.anchorMin        = new Vector2(0.5f, 0f);
            scoreRt.anchorMax        = new Vector2(1f, 1f);
            scoreRt.offsetMin        = new Vector2(0f, 0f);
            scoreRt.offsetMax        = new Vector2(-32f, -10f);
            var scoreTmp = scoreRt.gameObject.AddComponent<TextMeshProUGUI>();
            scoreTmp.text               = "0";
            scoreTmp.fontSize           = 44;
            scoreTmp.fontStyle          = FontStyles.Bold;
            scoreTmp.color              = Color.white;
            scoreTmp.alignment          = TextAlignmentOptions.MidlineRight;
            scoreTmp.enableWordWrapping = false;
            scoreTmp.raycastTarget      = false;

            // ── HudManager component ───────────────────────────────────────────
            var hud = canvasGO.AddComponent<HudManager>();
            SetField(hud, "levelText", levelTmp);
            SetField(hud, "scoreText", scoreTmp);
            // scoreManager must be wired in Inspector (it's on the Manager GO)

            // HUD starts inactive — SceneFlowController activates it during gameplay.
            canvasGO.SetActive(false);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[HudUIBuilder] HudCanvas created. Wire ScoreManager ref in Inspector.");
            Selection.activeGameObject = canvasGO;
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"[HudUIBuilder] Field '{fieldName}' not found.");
        }
    }
}
