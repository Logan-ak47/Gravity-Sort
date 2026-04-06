using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Editor tool: GravitySort → Create Level Complete UI
    /// Builds the LevelCompletePopup Canvas hierarchy in the active scene,
    /// creates all UI elements, and wires all SerializeField references.
    /// </summary>
    public static class LevelCompleteUIBuilder
    {
        // ── Colors ─────────────────────────────────────────────────────────────
        private static readonly Color PanelBg      = new Color(0.08f, 0.09f, 0.18f, 0.97f);
        private static readonly Color OverlayColor = new Color(0.00f, 0.00f, 0.00f, 0.72f);
        private static readonly Color White         = Color.white;
        private static readonly Color Gold          = new Color(1.00f, 0.85f, 0.10f, 1.00f);
        private static readonly Color ButtonGreen   = new Color(0.20f, 0.72f, 0.35f, 1.00f);
        private static readonly Color ButtonGrey    = new Color(0.28f, 0.30f, 0.35f, 1.00f);
        private static readonly Color SubText       = new Color(0.65f, 0.70f, 0.80f, 1.00f);

        [MenuItem("GravitySort/Create Level Complete UI")]
        public static void Build()
        {
            // Remove stale instance
            var existing = GameObject.Find("LevelCompleteCanvas");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[LevelCompleteUIBuilder] Removed existing LevelCompleteCanvas.");
            }

            // ── Canvas ─────────────────────────────────────────────────────────
            var canvasGO = new GameObject("LevelCompleteCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Level Complete UI");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Overlay (full-screen blocker behind panel) ─────────────────────
            var overlay = MakeRect("Overlay", canvasGO.transform);
            Stretch(overlay);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color          = OverlayColor;
            overlayImg.raycastTarget  = true;

            // ── Panel ─────────────────────────────────────────────────────────
            var panel = MakeRect("Panel", canvasGO.transform);
            SetRect(panel, Vector2.zero, new Vector2(560, 720));
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = PanelBg;

            // ── Title ─────────────────────────────────────────────────────────
            var titleText = MakeTMP("TitleText", panel,
                "Level Complete!", 52, White, FontStyles.Bold,
                new Vector2(0, 270), new Vector2(500, 80));

            // ── Score label ───────────────────────────────────────────────────
            MakeTMP("ScoreLabel", panel,
                "SCORE", 26, SubText, FontStyles.Normal,
                new Vector2(0, 175), new Vector2(400, 50));

            // ── Score value ───────────────────────────────────────────────────
            var scoreText = MakeTMP("ScoreText", panel,
                "0", 64, White, FontStyles.Bold,
                new Vector2(0, 105), new Vector2(400, 90));

            // ── Stars row ─────────────────────────────────────────────────────
            var starsRow = MakeRect("StarsRow", panel);
            SetRect(starsRow, new Vector2(0, 5), new Vector2(300, 80));

            var star1 = MakeStar("Star1", starsRow, new Vector2(-100, 0));
            var star2 = MakeStar("Star2", starsRow, new Vector2(   0, 0));
            var star3 = MakeStar("Star3", starsRow, new Vector2( 100, 0));

            // ── Coins text ────────────────────────────────────────────────────
            var coinText = MakeTMP("CoinsText", panel,
                "+150 coins", 32, Gold, FontStyles.Normal,
                new Vector2(0, -80), new Vector2(400, 55));

            // ── Buttons row ───────────────────────────────────────────────────
            var btnsRow = MakeRect("ButtonsRow", panel);
            SetRect(btnsRow, new Vector2(0, -230), new Vector2(500, 80));

            var (nextBtn, _) = MakeButton("NextButton", btnsRow,
                "Next Level", ButtonGreen, new Vector2(-130, 0), new Vector2(230, 72));
            var (menuBtn, _) = MakeButton("MenuButton", btnsRow,
                "Menu", ButtonGrey, new Vector2( 130, 0), new Vector2(230, 72));

            // ── Wire LevelCompletePopup ────────────────────────────────────────
            var popup = canvasGO.AddComponent<LevelCompletePopup>();
            SetPrivateField(popup, "panel",     panel);
            SetPrivateField(popup, "scoreText", scoreText.GetComponent<TextMeshProUGUI>());
            SetPrivateField(popup, "coinText",  coinText.GetComponent<TextMeshProUGUI>());
            SetPrivateField(popup, "starImages", new Image[] { star1, star2, star3 });
            SetPrivateField(popup, "nextButton", nextBtn);
            SetPrivateField(popup, "menuButton", menuBtn);

            // scoreManager must be wired manually in the Inspector (it's on Manager GO)

            // Start hidden
            canvasGO.SetActive(false);

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[LevelCompleteUIBuilder] Canvas created. " +
                      "Wire ScoreManager ref on LevelCompletePopup in the Inspector.");
            Selection.activeGameObject = canvasGO;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        /// <summary>Stretch to fill parent (anchor 0,0 → 1,1, offsets 0).</summary>
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
            rt.pivot      = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Center-anchored rect at given position with given size.</summary>
        private static void SetRect(RectTransform rt, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
        }

        private static RectTransform MakeTMP(string name, RectTransform parent,
            string text, float fontSize, Color color, FontStyles style,
            Vector2 pos, Vector2 size)
        {
            var rt = MakeRect(name, parent);
            SetRect(rt, pos, size);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text           = text;
            tmp.fontSize       = fontSize;
            tmp.color          = color;
            tmp.fontStyle      = style;
            tmp.alignment      = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return rt;
        }

        private static Image MakeStar(string name, RectTransform parent, Vector2 pos)
        {
            var rt = MakeRect(name, parent);
            SetRect(rt, pos, new Vector2(72, 72));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.30f, 1f); // inactive grey by default
            return img;
        }

        private static (Button button, TextMeshProUGUI label) MakeButton(
            string name, RectTransform parent,
            string label, Color bgColor, Vector2 pos, Vector2 size)
        {
            var rt = MakeRect(name, parent);
            SetRect(rt, pos, size);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = bgColor;

            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor;
            colors.highlightedColor = bgColor * 1.15f;
            colors.pressedColor     = bgColor * 0.80f;
            btn.colors = colors;

            // Label child
            var labelRt = MakeRect(name + "Text", rt);
            Stretch(labelRt);
            var tmp = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 32;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            return (btn, tmp);
        }

        /// <summary>Sets a private/serialized field via reflection (Editor only).</summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"[LevelCompleteUIBuilder] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
