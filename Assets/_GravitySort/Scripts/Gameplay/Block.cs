using DG.Tweening;
using UnityEngine;

namespace GravitySort
{
    public class Block : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public int colorIndex { get; private set; }

        private GameConfig config;
        private Vector3 baseScale = Vector3.one;

        // ── Pooling ────────────────────────────────────────────────────────────

        public void Init(GameConfig gameConfig)
        {
            config = gameConfig;
            transform.localScale = baseScale;
            SetAlpha(1f);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Sets the block's base (resting) scale. Called by GridManager before
        /// activating the block so all scale-relative operations use the right size.
        /// </summary>
        public void SetBaseScale(float size)
        {
            baseScale            = new Vector3(size, size, 1f);
            transform.localScale = baseScale;
        }

        public void ResetBlock()
        {
            DOTween.Kill(transform);
            transform.localScale = baseScale;
            SetAlpha(1f);
            gameObject.SetActive(false);
        }

        // ── Color ──────────────────────────────────────────────────────────────

        public void SetColor(int index, GameConfig gameConfig)
        {
            colorIndex = index;
            config = gameConfig;
            spriteRenderer.color = config.blockColors[index];
        }

        // ── Selection ──────────────────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            DOTween.Kill(transform);
            transform.localScale = baseScale;

            if (selected)
            {
                transform
                    .DOScale(baseScale * config.selectionPulseScale, config.selectionPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        // ── Animations ─────────────────────────────────────────────────────────

        public void PlayClearAnimation(System.Action onComplete)
        {
            DOTween.Kill(transform);

            float halfDuration = config.clearDuration * 0.5f;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(baseScale * 1.2f, halfDuration).SetEase(Ease.OutQuad));
            seq.Append(spriteRenderer.DOFade(0f, halfDuration));
            seq.OnComplete(() =>
            {
                ResetBlock();
                onComplete?.Invoke();
            });
        }

        public void PlayDropAnimation(Vector3 target, float duration, System.Action onComplete)
        {
            DOTween.Kill(transform);

            transform
                .DOMove(target, duration)
                .SetEase(Ease.OutBounce)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayPourArc(Vector3 target, float arcHeight, float duration, System.Action onComplete)
        {
            DOTween.Kill(transform);

            transform
                .DOJump(target, arcHeight, 1, duration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SetAlpha(float alpha)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
