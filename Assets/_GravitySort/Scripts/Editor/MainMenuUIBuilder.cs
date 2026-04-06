using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GravitySort
{
    /// <summary>
    /// Editor tool: GravitySort → Create Main Menu UI
    /// Builds the MainMenu Canvas. After running, wire LevelSelectUI in the Inspector.
    /// </summary>
    public static class MainMenuUIBuilder
    {
        private static readonly Color NavyBg      = new Color(0.102f, 0.102f, 0.180f, 1f); // #1A1A2E
        private static readonly Color PlayBlue    = new Color(0.302f, 0.651f, 1.000f, 1f); // #4DA6FF
        private static readonly Color SettingsBg  = new Color(0.15f,  0.15f,  0.25f,  0.8f);
        private static readonly Color SubtleText  = new Color(0.55f,  0.60f,  0.72f,  1f);

        [MenuItem("GravitySort/Create Main Menu UI")]
        public static void Build()
        {
            var existing = GameObject.Find("MainMenuCanvas");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[MainMenuUIBuilder] Removed existing MainMenuCanvas.");
            }

            // ── Canvas ─────────────────────────────────────────────────────────
            var canvasGO = new GameObject("MainMenuCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Main Menu UI");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1; // below level-select (5) and popups (10)

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Background ─────────────────────────────────────────────────────
            var bgRt = MakeRect("Background", canvasGO.transform);
            Stretch(bgRt);
            bgRt.gameObject.AddComponent<Image>().color = NavyBg;

            // ── Settings button (top-right corner) ─────────────────────────────
            var settingsRt = MakeRect("SettingsButton", canvasGO.transform);
            settingsRt.anchorMin        = new Vector2(1f, 1f);
            settingsRt.anchorMax        = new Vector2(1f, 1f);
            settingsRt.pivot            = new Vector2(1f, 1f);
            settingsRt.anchoredPosition = new Vector2(-28f, -40f);
            settingsRt.sizeDelta        = new Vector2(88f, 88f);

            var settingsImg = settingsRt.gameObject.AddComponent<Image>();
            settingsImg.color = SettingsBg;
            var settingsBtn = settingsRt.gameObject.AddComponent<Button>();
            var sColors = settingsBtn.colors;
            sColors.highlightedColor = SettingsBg * 1.3f;
            sColors.pressedColor     = SettingsBg * 0.7f;
            settingsBtn.colors = sColors;

            // Gear icon via Unicode ⚙ (U+2699) — renders fine with TMP default font
            var gearRt  = MakeRect("GearIcon", settingsRt);
            Stretch(gearRt);
            var gearTmp = gearRt.gameObject.AddComponent<TextMeshProUGUI>();
            gearTmp.text      = "\u2699";
            gearTmp.fontSize  = 48;
            gearTmp.color     = new Color(0.75f, 0.80f, 0.90f, 1f);
            gearTmp.alignment = TextAlignmentOptions.Center;
            gearTmp.raycastTarget = false;

            // ── Title block ────────────────────────────────────────────────────
            // Positioned in upper-centre, leaving room for subtitle and play button below.
            var titleBlockRt = MakeRect("TitleBlock", canvasGO.transform);
            titleBlockRt.anchorMin        = new Vector2(0f, 0.5f);
            titleBlockRt.anchorMax        = new Vector2(1f, 0.5f);
            titleBlockRt.pivot            = new Vector2(0.5f, 0.5f);
            titleBlockRt.anchoredPosition = new Vector2(0f, 280f);
            titleBlockRt.sizeDelta        = new Vector2(0f, 220f);

            // Main title
            var titleRt  = MakeRect("TitleText", titleBlockRt);
            titleRt.anchorMin = new Vector2(0f, 0.45f);
            titleRt.anchorMax = new Vector2(1f, 1.00f);
            titleRt.offsetMin = new Vector2(32f, 0f);
            titleRt.offsetMax = new Vector2(-32f, 0f);
            var titleTmp = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            titleTmp.text               = "GRAVITY SORT";
            titleTmp.fontSize           = 86;
            titleTmp.fontStyle          = FontStyles.Bold;
            titleTmp.color              = Color.white;
            titleTmp.alignment          = TextAlignmentOptions.Center;
            titleTmp.enableWordWrapping = false;
            titleTmp.characterSpacing   = 6f;

            // Subtitle
            var subRt  = MakeRect("SubtitleText", titleBlockRt);
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.45f);
            subRt.offsetMin = new Vector2(32f, 0f);
            subRt.offsetMax = new Vector2(-32f, 0f);
            var subTmp = subRt.gameObject.AddComponent<TextMeshProUGUI>();
            subTmp.text               = "Color Sort Puzzle";
            subTmp.fontSize           = 34;
            subTmp.fontStyle          = FontStyles.Italic;
            subTmp.color              = SubtleText;
            subTmp.alignment          = TextAlignmentOptions.Center;
            subTmp.enableWordWrapping = false;

            // ── PLAY button ────────────────────────────────────────────────────
            var playRt = MakeRect("PlayButton", canvasGO.transform);
            playRt.anchorMin        = new Vector2(0.5f, 0.5f);
            playRt.anchorMax        = new Vector2(0.5f, 0.5f);
            playRt.pivot            = new Vector2(0.5f, 0.5f);
            playRt.anchoredPosition = new Vector2(0f, -120f);
            playRt.sizeDelta        = new Vector2(420f, 120f);

            var playImg = playRt.gameObject.AddComponent<Image>();
            playImg.color = PlayBlue;
            var playBtn = playRt.gameObject.AddComponent<Button>();
            var pColors = playBtn.colors;
            pColors.normalColor      = PlayBlue;
            pColors.highlightedColor = PlayBlue * 1.15f;
            pColors.pressedColor     = PlayBlue * 0.80f;
            playBtn.colors = pColors;

            var playTextRt  = MakeRect("PlayText", playRt);
            Stretch(playTextRt);
            var playTmp = playTextRt.gameObject.AddComponent<TextMeshProUGUI>();
            playTmp.text             = "PLAY";
            playTmp.fontSize         = 52;
            playTmp.fontStyle        = FontStyles.Bold;
            playTmp.color            = Color.white;
            playTmp.alignment        = TextAlignmentOptions.Center;
            playTmp.characterSpacing = 8f;
            playTmp.raycastTarget    = false;

            // ── Version label (bottom-center) ──────────────────────────────────
            var verRt  = MakeRect("VersionText", canvasGO.transform);
            verRt.anchorMin        = new Vector2(0f, 0f);
            verRt.anchorMax        = new Vector2(1f, 0f);
            verRt.pivot            = new Vector2(0.5f, 0f);
            verRt.anchoredPosition = new Vector2(0f, 30f);
            verRt.sizeDelta        = new Vector2(0f, 50f);
            var verTmp = verRt.gameObject.AddComponent<TextMeshProUGUI>();
            verTmp.text      = "v0.1 — MVP";
            verTmp.fontSize  = 22;
            verTmp.color     = new Color(0.35f, 0.38f, 0.50f, 1f);
            verTmp.alignment = TextAlignmentOptions.Center;

            // ── Wire MainMenu component ────────────────────────────────────────
            var mainMenu = canvasGO.AddComponent<MainMenu>();
            SetField(mainMenu, "playButton",     playBtn);
            SetField(mainMenu, "settingsButton", settingsBtn);
            // levelSelectUI must be wired manually — it's on a separate Canvas

            canvasGO.SetActive(false);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[MainMenuUIBuilder] MainMenuCanvas created. " +
                      "Wire LevelSelectUI ref on MainMenu in the Inspector.");
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

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"[MainMenuUIBuilder] Field '{fieldName}' not found.");
        }
    }
}
