using UnityEngine;

namespace GravitySort
{
    public enum WinConditionType { ClearAll, ReduceBelow, Survive }

    [System.Serializable]
    public struct StartingBlock
    {
        public int column;     // 0-based column index
        public int colorIndex; // 0-based color index
    }

    [System.Serializable]
    public struct DropEntry
    {
        public int column;     // which column this block drops into
        public int colorIndex; // what color
    }

    [CreateAssetMenu(fileName = "Level_XX", menuName = "GravitySort/LevelData")]
    public class LevelData : ScriptableObject
    {
        public int levelNumber;
        public int columnCount = 6;        // 3-6 columns
        public int maxHeight = 8;          // rows per column
        public int colorCount = 3;         // 2-5 active colors
        public float dropInterval = 4.0f;  // seconds between drops
        public WinConditionType winCondition = WinConditionType.ClearAll;
        public int winTarget = 0;          // for ReduceBelow: target count
        public StartingBlock[] startingBlocks;
        public DropEntry[] dropSequence;   // pre-determined drop order
        public int maxDrops = 50;          // total blocks that will drop (finite)
    }
}
