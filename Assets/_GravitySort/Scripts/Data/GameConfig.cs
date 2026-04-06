using UnityEngine;

namespace GravitySort
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "GravitySort/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Block Colors")]
        public Color[] blockColors = new Color[]
        {
            new Color(1f, 0.3f, 0.42f),     // Index 0 — Red    #FF4D6A
            new Color(0.3f, 0.65f, 1f),     // Index 1 — Blue   #4DA6FF
            new Color(0.36f, 0.86f, 0.43f), // Index 2 — Green  #5BDB6E
            new Color(1f, 0.85f, 0.3f),     // Index 3 — Yellow #FFD84D
            new Color(0.7f, 0.3f, 1f)       // Index 4 — Purple #B44DFF
        };

        [Header("Grid")]
        public float gridPaddingPercent = 0.05f;
        public float blockSpacing = 0.05f;
        public float gridBottomOffset = -2f;

        [Header("Animation Timing")]
        public float pourArcHeight = 1.5f;
        public float pourBlockDuration = 0.2f;
        public float pourStagger = 0.08f;
        public float dropDuration = 0.4f;
        public float clearDuration = 0.3f;
        public float settleDuration = 0.2f;
        public float selectionPulseScale = 1.08f;
        public float selectionPulseDuration = 0.4f;

        [Header("Gameplay")]
        public int matchThreshold = 3;
        public int previewCount = 3;

        [Header("Boosters Per Level")]
        public int freeUndo = 2;
        public int freeFreeze = 1;
        public int freeBomb = 1;
        public float freezeDuration = 10f;

        [Header("Economy")]
        public int undoGemCost = 5;
        public int freezeGemCost = 8;
        public int bombGemCost = 10;
        public int continueGemCost = 5;
    }
}
