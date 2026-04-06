using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GravitySort
{
    public class InputHandler : MonoBehaviour
    {
        // ── References ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GridManager gridManager;

        // ── State ──────────────────────────────────────────────────────────────

        [Header("State")]
        public bool inputEnabled = true;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired with the 0-based column index when the player taps a valid column.
        /// Not fired if input is disabled or the tap lands outside the grid.
        /// </summary>
        public event System.Action<int> OnColumnTapped;

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!inputEnabled) return;

            // Mouse — editor and standalone
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                HandleTap(mouse.position.ReadValue());
                return;
            }

            // Touch — mobile
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
                HandleTap(touchscreen.primaryTouch.position.ReadValue());
        }

        // ── Tap handling ───────────────────────────────────────────────────────

        private void HandleTap(Vector2 screenPosition)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));

            int column = gridManager.GetColumnForWorldX(world.x);
            if (column >= 0)
                OnColumnTapped?.Invoke(column);
        }
    }
}
