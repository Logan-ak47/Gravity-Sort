using UnityEngine;

namespace GravitySort
{
    public class TestBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelData          levelData;
        [SerializeField] private GridManager        gridManager;
        [SerializeField] private BlockDropper       blockDropper;
        [SerializeField] private GameplayController gameplayController;
        [SerializeField] private NextBlockPreview   nextBlockPreview;

        private void Start()
        {
            // Signal GameManager that a level is loading
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCurrentLevel(levelData.levelNumber);
                GameManager.Instance.ChangeState(GameState.LoadingLevel);
            }

            // Initialise grid and spawn starting blocks
            gridManager.InitGrid(levelData.columnCount, levelData.maxHeight);
            gridManager.SpawnInitialBlocks(levelData.startingBlocks);

            // Arm drop sequence and win detection
            blockDropper.SetLevel(levelData);
            gameplayController.SetLevel(levelData);

            // Show initial upcoming drops before first timer fires
            nextBlockPreview.Refresh();

            // Hand control to the player
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);
        }
    }
}
