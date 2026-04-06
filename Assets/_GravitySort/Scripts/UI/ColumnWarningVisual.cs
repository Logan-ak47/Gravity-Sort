using DG.Tweening;
using UnityEngine;

namespace GravitySort
{
    /// <summary>
    /// Renders a semi-transparent red overlay behind any column that reaches the
    /// danger threshold (default: height >= maxRows - 1).  The overlay pulses
    /// between alpha 0.1 and 0.3 using a DOTween yoyo loop.
    ///
    /// Checks state every frame so it responds instantly to any game event
    /// (pour, drop, chain clear) without needing to subscribe to every event.
    /// The DOTween loop is only started/stopped on actual state transitions so
    /// there's no per-frame tween churn.
    ///
    /// Attach to any persistent GameObject. Assign overlaySprite (block_white.png)
    /// in the Inspector.
    /// </summary>
    public class ColumnWarningVisual : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Sprite      overlaySprite;   // block_white.png

        // ── Config ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Columns at this height or above show the warning. Default: maxRows - 1 (7 for an 8-row grid).")]
        [SerializeField] private int  dangerRow      = 7;
        [SerializeField] private float pulseMinAlpha  = 0.10f;
        [SerializeField] private float pulseMaxAlpha  = 0.30f;
        [SerializeField] private float pulseDuration  = 0.7f;   // half-cycle (yoyo)

        // ── Internals ──────────────────────────────────────────────────────────

        private SpriteRenderer[] overlays;    // one per column
        private bool[]           activeState; // true = warning currently showing

        private static readonly Color WarningColor = new Color(1f, 0.15f, 0.15f, 0f);

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            int cols   = gridManager.ColumnCount;
            overlays   = new SpriteRenderer[cols];
            activeState = new bool[cols];

            float cellSize  = gridManager.CellSize;
            float colHeight = gridManager.MaxRows * cellSize;
            float colWidth  = cellSize;

            for (int c = 0; c < cols; c++)
            {
                var go = new GameObject($"Warning_{c}");
                go.transform.SetParent(transform);

                var sr         = go.AddComponent<SpriteRenderer>();
                sr.sprite       = overlaySprite;
                sr.color        = WarningColor;    // starts fully transparent
                sr.sortingOrder = -1;              // behind all game blocks

                // Size the overlay to cover the full column
                Vector3 botCenter = gridManager.GetWorldPosition(c, 0);
                Vector3 topCenter = gridManager.GetWorldPosition(c, gridManager.MaxRows - 1);
                go.transform.position   = (botCenter + topCenter) * 0.5f;
                go.transform.localScale = new Vector3(colWidth, colHeight, 1f);

                overlays[c]    = sr;
                activeState[c] = false;
            }
        }

        private void Update()
        {
            // GetDangerColumns allocates a List each call — fine for a small column
            // count, but promote to a cached bool[] if profiler flags it later.
            System.Collections.Generic.List<int> dangerCols =
                gridManager.GetDangerColumns(dangerRow);

            for (int c = 0; c < overlays.Length; c++)
            {
                bool inDanger = dangerCols.Contains(c);

                if (inDanger && !activeState[c])
                    ActivateWarning(c);
                else if (!inDanger && activeState[c])
                    DeactivateWarning(c);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void ActivateWarning(int column)
        {
            activeState[column] = true;
            SpriteRenderer sr = overlays[column];

            DOTween.Kill(sr);

            Color start = WarningColor;
            start.a     = pulseMinAlpha;
            sr.color    = start;

            sr.DOFade(pulseMaxAlpha, pulseDuration)
              .SetEase(Ease.InOutSine)
              .SetLoops(-1, LoopType.Yoyo)
              .SetId(sr);   // keyed to the renderer so Kill(sr) stops exactly this tween
        }

        private void DeactivateWarning(int column)
        {
            activeState[column] = false;
            SpriteRenderer sr = overlays[column];

            DOTween.Kill(sr);

            // Snap alpha to 0 immediately — no fade-out needed
            Color c = sr.color;
            c.a      = 0f;
            sr.color = c;
        }
    }
}
