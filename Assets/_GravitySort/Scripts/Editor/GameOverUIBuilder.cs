using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Editor tool: GravitySort → Create Game Over UI
    /// Builds the GameOverPopup Canvas hierarchy and wires all SerializeField references.
    /// </summary>
    public static class GameOverUIBuilder
    {
        private static readonly Color PanelBg        = new Color(0.08f, 0.09f, 0.18f, 0.97f);
        private static readonly Color OverlayColor   = new Color(0.00f, 0.00f, 0.00f, 0.72f);
        private static readonly Color ButtonRed      = new Color(0.72f, 0.20f, 0.22f, 1.00f);
        private static readonly Color ButtonGreen    = new Color(0.20f, 0.72f, 0.35f, 1.00f);
        private static readonly Color ButtonGrey     = new Color(0.28f, 0.30f, 0.35f, 1.00f);
        private static readonly Color ButtonPurple   = new Color(0.45f, 0.20f, 0.72f, 1.00f);
        private static readonly Color SubText        = new Color(0.65f, 0.70f, 0.80f, 1.00f);
        private static readonly Color GemGold        = new Color(1.00f, 0.85f, 0.10f, 1.00f);
        private static readonly Color Divider        = new Color(1f, 1f, 1f, 0.10f);

        [MenuItem("GravitySort/Create Game Over UI")]
        public static void Build()
        {
            var existing = GameObject.Find("GameOverCanvas");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[GameOverUIBuilder] Removed existing GameOverCanvas.");
            }

            // ── Canvas ─────────────────────────────────────────────────────────
            var canvasGO = new GameObject("GameOverCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Game Over UI");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Overlay ────────────────────────────────────────────────────────
            var overlay = MakeRect("Overlay", canvasGO.transform);
            Stretch(overlay);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color         = OverlayColor;
            overlayImg.raycastTarget = true;

            // ── Panel (560 × 840) ──────────────────────────────────────────────
            var panel = MakeRect("Panel", canvasGO.transform);
            SetRect(panel, Vector2.zero, new Vector2(560, 840));
            panel.gameObject.AddComponent<Image>().color = PanelBg;

            // ── Title ─────────────────────────────────────────────────────────
            MakeTMP("TitleText", panel,
                "Game Over", 56, new Color(0.90f, 0.30f, 0.30f, 1f), FontStyles.Bold,
                new Vector2(0, 360), new Vector2(500, 85));

            // ── Score ─────────────────────────────────────────────────────────
            MakeTMP("ScoreLabel", panel,
                "SCORE", 26, SubText, FontStyles.Normal,
                new Vector2(0, 275), new Vector2(400, 50));

            var scoreText = MakeTMP("ScoreText", panel,
                "0", 64, Color.white, FontStyles.Bold,
                new Vector2(0, 205), new Vector2(400, 85));

            // ── Divider ───────────────────────────────────────────────────────
            MakeDivider("Divider1", panel, new Vector2(0, 150));

            // ── "Continue?" header ────────────────────────────────────────────
            MakeTMP("ContinueLabel", panel,
                "Continue?", 30, SubText, FontStyles.Bold,
                new Vector2(0, 110), new Vector2(400, 50));

            // ── Gem row ───────────────────────────────────────────────────────
            var gemRow = MakeRect("GemRow", panel);
            SetRect(gemRow, new Vector2(0, 50), new Vector2(500, 64));

            // Gem icon (small coloured square — swap for real gem sprite in Inspector)
            var gemIconRt = MakeRect("GemIcon", gemRow);
            SetRect(gemIconRt, new Vector2(-180, 0), new Vector2(40, 40));
            var gemIcon = gemIconRt.gameObject.AddComponent<Image>();
            gemIcon.color = GemGold;

            // "Remove top 2 rows for 5 gems" label
            var gemContinueLabel = MakeTMP("GemContinueLabel", gemRow,
                "Remove top 2 rows — 5 gems", 24, Color.white, FontStyles.Normal,
                new Vector2(20, 0), new Vector2(300, 48));

            // Gem continue button
            var (gemBtn, _) = MakeButton("GemContinueButton", panel,
                "Use Gems", ButtonGreen,
                new Vector2(0, -20), new Vector2(440, 68));

            // ── Ad continue button ─────────────────────────────────────────────
            var (adBtn, _) = MakeButton("AdContinueButton", panel,
                "Watch Ad to Continue", ButtonPurple,
                new Vector2(0, -105), new Vector2(440, 68));

            // ── Divider ───────────────────────────────────────────────────────
            MakeDivider("Divider2", panel, new Vector2(0, -165));

            // ── Gem balance display ────────────────────────────────────────────
            var gemBalanceText = MakeTMP("GemBalanceText", panel,
                "50 gems", 24, GemGold, FontStyles.Normal,
                new Vector2(0, -205), new Vector2(400, 44));

            // ── Primary buttons row ───────────────────────────────────────────
            var btnsRow = MakeRect("ButtonsRow", panel);
            SetRect(btnsRow, new Vector2(0, -320), new Vector2(500, 80));

            var (tryBtn, _) = MakeButton("TryAgainButton", btnsRow,
                "Try Again", ButtonRed,
                new Vector2(-130, 0), new Vector2(228, 72));
            var (menuBtn, _) = MakeButton("MenuButton", btnsRow,
                "Menu", ButtonGrey,
                new Vector2(130, 0), new Vector2(228, 72));

            // ── Wire GameOverPopup ─────────────────────────────────────────────
            var popup = canvasGO.AddComponent<GameOverPopup>();
            SetField(popup, "panel",             panel);
            SetField(popup, "scoreText",         scoreText.GetComponent<TextMeshProUGUI>());
            SetField(popup, "gemBalanceText",    gemBalanceText.GetComponent<TextMeshProUGUI>());
            SetField(popup, "gemContinueButton", gemBtn);
            SetField(popup, "gemContinueLabel",  gemContinueLabel.GetComponent<TextMeshProUGUI>());
            SetField(popup, "adContinueButton",  adBtn);
            SetField(popup, "tryAgainButton",    tryBtn);
            SetField(popup, "menuButton",        menuBtn);
            // scoreManager / gridManager / blockDropper / inputHandler
            // must be wired manually (they're on the Manager / GridManager GOs)

            canvasGO.SetActive(false);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[GameOverUIBuilder] Canvas created. " +
                      "Wire ScoreManager, GridManager, BlockDropper, InputHandler refs in Inspector.");
            Selection.activeGameObject = canvasGO;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        private static RectTransform MakeTMP(string name, RectTransform parent,
            string text, float size, Color color, FontStyles style,
            Vector2 pos, Vector2 rectSize)
        {
            var rt  = MakeRect(name, parent);
            SetRect(rt, pos, rectSize);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = size;
            tmp.color              = color;
            tmp.fontStyle          = style;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return rt;
        }

        private static void MakeDivider(string name, RectTransform parent, Vector2 pos)
        {
            var rt  = MakeRect(name, parent);
            SetRect(rt, pos, new Vector2(480, 2));
            rt.gameObject.AddComponent<Image>().color = Divider;
        }

        private static (Button button, TextMeshProUGUI label) MakeButton(
            string name, RectTransform parent,
            string label, Color bgColor, Vector2 pos, Vector2 size)
        {
            var rt  = MakeRect(name, parent);
            SetRect(rt, pos, size);

            var img    = rt.gameObject.AddComponent<Image>();
            img.color  = bgColor;

            var btn    = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor;
            colors.highlightedColor = bgColor * 1.15f;
            colors.pressedColor     = bgColor * 0.80f;
            colors.disabledColor    = bgColor * 0.4f;
            btn.colors = colors;

            var labelRt = MakeRect(name + "Text", rt);
            Stretch(labelRt);
            var tmp       = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 30;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            return (btn, tmp);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"[GameOverUIBuilder] Field '{fieldName}' not found.");
        }
    }
}
