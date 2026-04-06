# Gravity Sort — Claude Code Project Context

## Project Overview
Color-sort puzzle meets falling blocks (Water Sort + Tetris gravity). Mobile (iOS + Android), portrait 9:16. Solo dev MVP: 30 levels, core loop + 3 boosters.
- GDD: `Assets/GravitySort_GDD_Complete.md` — authoritative reference for ALL decisions

## Tech Stack
| Item | Value |
|---|---|
| Engine | Unity 6 (6000.3.8f1), 2D URP |
| Input | **New Input System** package (NOT legacy `UnityEngine.Input`) |
| Animation | **DOTween** only — no Unity Animator |
| Namespace | `GravitySort` on all scripts |
| Config | All values in `GameConfig` ScriptableObject — no magic numbers in code |

## Architecture Rules (GDD §6.4)
1. **Data ≠ Visuals** — `GridManager` holds `List<int>[]` column data. `Block` MonoBehaviours are visual only. Never read game state from positions.
2. **Animation locks input** — `inputHandler.inputEnabled = false` during POURING/CLEARING/SETTLING.
3. **Event-driven** — `OnPourComplete`, `OnMatchCleared`, `OnSettleComplete`, `OnBlockDropped`, `OnColumnOverflow`
4. **Object pooling** — Pool of ~60 Block objects created in `InitGrid()`. Never Instantiate/Destroy at runtime.
5. **ScriptableObjects for config** — `GameConfig.asset` holds everything.
6. **DOTween for all animations** — Kill tweens before starting new ones with `DOTween.Kill(transform)`.
7. **LEVEL DESIGN RULE — Color Count Divisibility:** For ClearAll levels, total count of each color (startingBlocks + dropSequence) must be divisible by `matchThreshold` (default 3). Guarantees mathematical clearability. Validate every level before shipping. Example: 6R 6B 6G ✓ — 7R ✗.

## Camera & Grid Config
| Property | Value |
|---|---|
| `Camera.orthographicSize` | 6 |
| `Camera.clearFlags` | SolidColor |
| `Camera.backgroundColor` | #1A1A2E (deep navy) |
| `GameConfig.gridPaddingPercent` | 0.20 |
| `GameConfig.gridBottomOffset` | -3.6 |
| Cell size at runtime | 0.9 world units (6 columns, 9:16) |
| Block visual size | `CellSize * 0.85f` (set via `Block.SetBaseScale()`) |

**Position formula:**
```
usableWidth = camWidth * (1 - gridPaddingPercent)
cellSize    = usableWidth / columnCount
gridLeft    = -(usableWidth * 0.5f)
x = gridLeft + col * cellSize + cellSize * 0.5f
y = gridBottomOffset + row * cellSize + cellSize * 0.5f
```

## Block Colors (GDD §5.2)
| Index | Color | Hex |
|---|---|---|
| 0 | Red | #FF4D6A |
| 1 | Blue | #4DA6FF |
| 2 | Green | #5BDB6E |
| 3 | Yellow | #FFD84D |
| 4 | Purple | #B44DFF |

## Scripts Built
| File | Path | Purpose |
|---|---|---|
| `GameConfig.cs` | `Scripts/Data/` | ScriptableObject: all config values |
| `LevelData.cs` | `Scripts/Data/` | ScriptableObject: level definition, `StartingBlock[]`, `DropEntry[]`, `WinConditionType` |
| `Block.cs` | `Scripts/Gameplay/` | Single block visual + DOTween animations; `SetBaseScale()`, `SetSelected()`, `ResetBlock()`, `PlayClearAnimation()`, `PlayDropAnimation()`, `PlayPourArc()` |
| `GridManager.cs` | `Scripts/Gameplay/` | Column data (`List<int>[]`), block pool, world positioning, `CellSize`, all CRUD, `SettleColumn()`, `SettleAllColumns()` |
| `InputHandler.cs` | `Scripts/Gameplay/` | New Input System tap detection; fires `OnColumnTapped` event; `inputEnabled` toggle |
| `GameplayController.cs` | `Scripts/Gameplay/` | Full pour flow: selection → `CanPour` → `StartPour` → `ExecutePour` (data-only) → `PourAnimator` → `SettleColumn` → unlock input |
| `PourAnimator.cs` | `Scripts/Gameplay/` | Animates grabbed `Block[]` arcing to dest via `DOJump`, staggered, landing bounce, `SetBlockVisual` on land |
| `TestBootstrap.cs` | `Scripts/Gameplay/` | Temporary test harness — calls `InitGrid()` + `SpawnInitialBlocks()` on Start. **Delete when GameManager/LevelManager built.** |
| `BlockPrefabCreator.cs` | `Scripts/Editor/` | Menu: GravitySort → Create Block Prefab. Generates `block_white.png` (PPU=32, 1×1 world unit) |
| `GameConfigCreator.cs` | `Scripts/Editor/` | Menu: GravitySort → Create GameConfig Asset |

## Key Public APIs

### GridManager
```csharp
void    InitGrid(int columns, int maxHeight)
void    SpawnBlockAt(int column, int row, int colorIndex)
void    SpawnInitialBlocks(StartingBlock[] startingBlocks)
float   CellSize { get; }                               // world units per cell
Vector3 GetWorldPosition(int column, int row)
int     GetColumnForWorldX(float worldX)                // returns -1 if outside grid
Block[] GetTopColorGroupBlocks(int column)
(int colorIndex, int count) GetTopColorGroup(int column)
int     GetTopColor(int column)                         // -1 if empty
int     GetColumnHeight(int column)
int     MaxRows { get; }
bool    IsColumnFull(int column)
bool    IsColumnEmpty(int column)
void    AddBlockToColumn(int column, int colorIndex)    // data + visual
void    RemoveBlocksFromTop(int column, int count)      // data + deactivates visuals
void    RemoveBlocksFromTopDataOnly(int column, int count) // data + clears slots, keeps Block alive
void    AddBlockToColumnData(int column, int colorIndex)   // data only, no visual spawn
void    SetBlockVisual(int column, int row, Block block)   // register animated block at landing cell
void    RefreshVisuals()                                // snap all blocks to data positions
void    SettleColumn(int column, Action onComplete)     // DOMove out-of-place blocks to correct rows
void    SettleAllColumns(Action onComplete)             // parallel settle on all columns
```

### GameplayController pour flow
```
HandleColumnTapped → Select (first tap) / Deselect (same) / StartPour (different + CanPour)
StartPour:
  1. inputEnabled = false
  2. GetTopColorGroupBlocks (BEFORE data change)
  3. SetSelected(false) on grabbed blocks
  4. ExecutePour (data-only: RemoveBlocksFromTopDataOnly + AddBlockToColumnData)
  5. PourAnimator.AnimatePour(blocks, count, destCol, callback)
  6. callback → SettleColumn(sourceCol) → inputEnabled = true
```

### PourAnimator
```csharp
void AnimatePour(Block[] blocks, int blockCount, int destCol, Action onComplete)
// destStartRow = GetColumnHeight(destCol) - blockCount  (post-ExecutePour)
// blocks[0] = topmost → lands highest row
// SetBlockVisual called per block on landing
// RefreshVisuals + onComplete after last block lands
```

### Block
```csharp
void Init(GameConfig config)
void SetBaseScale(float size)          // sets baseScale field + transform.localScale
void SetColor(int index, GameConfig config)
void SetSelected(bool selected)        // DOTween pulse from baseScale
void ResetBlock()                      // kill tweens, reset to baseScale, deactivate
void PlayClearAnimation(Action onComplete)
void PlayDropAnimation(Vector3 target, float duration, Action onComplete)
void PlayPourArc(Vector3 target, float arcHeight, float duration, Action onComplete)
int  colorIndex { get; }
```

### InputHandler
```csharp
public event Action<int> OnColumnTapped;
public bool inputEnabled;
```

## Assets
| Asset | Path |
|---|---|
| GameConfig | `Assets/_GravitySort/ScriptableObjects/GameConfig.asset` |
| Block prefab | `Assets/_GravitySort/Prefabs/Block.prefab` |
| Block sprite | `Assets/_GravitySort/Sprites/block_white.png` (32×32 px, PPU=32 → 1×1 world unit) |
| Test level | `Assets/_GravitySort/ScriptableObjects/Levels/Level_Test.asset` |

## Scene: SampleScene (Assets/Scenes/SampleScene.unity)
| GameObject | Components | Key References |
|---|---|---|
| `Main Camera` | Camera, UniversalAdditionalCameraData, Volume | orthographic size 6, bg #1A1A2E |
| `Directional Light` | Light | — |
| `Global Volume` | Volume | — |
| `Bootstrap` | TestBootstrap | levelData=Level_Test, gridManager=GridManager |
| `GridManager` | GridManager | config=GameConfig, blockPrefab=Block.prefab |
| `Manager` | InputHandler, GameplayController, PourAnimator | all cross-references wired (see below) |

**Manager wiring:**
- `InputHandler.gridManager` → GridManager
- `GameplayController.gridManager` → GridManager
- `GameplayController.inputHandler` → InputHandler (on Manager)
- `GameplayController.pourAnimator` → PourAnimator (on Manager)
- `PourAnimator.gridManager` → GridManager
- `PourAnimator.config` → GameConfig

## Current Playable State
- Grid renders 14 test blocks in correct positions and colors
- Tap selects a column (top color group pulses); tap same column deselects
- Tapping a valid destination pours blocks with staggered arc animation + landing bounce
- Source column blocks animate down to correct positions after pour (OutBounce settle)
- Input is locked during animation, re-enabled on complete
- **Cannot** match/clear, drop blocks, or progress levels yet

## Critical Path — What's Next

### Phase 1 remaining
1. **MatchChecker.cs** — scan each column for a full column of same color → trigger clear
2. **ClearAnimation** — `Block.PlayClearAnimation()` on matched blocks, then pool them
3. **Chain reactions** — after settle, recheck for new matches
4. **GameManager.cs** — formal state machine (PLAYING → POURING → SETTLING → CHECKING → PLAYING)

### Phase 2
5. **ChainReactionHandler.cs** — clear → `SettleAllColumns` → recheck loop, combo counter

### Phase 3
6. **BlockDropper.cs** — timer, `LevelData.dropSequence`, drop animation from above, overflow detection

### Phase 4+
7. Level assets (30 levels), win/lose popups, LevelManager, UI

## GDD Phase Status
| Phase | Description | Status |
|---|---|---|
| 1 | Grid + Sort | ~75% — grid, selection, full pour + settle working |
| 2 | Match + Chains | 0% |
| 3 | Block Drops | 0% |
| 4 | Levels + Flow | 0% |
| 5 | Polish | 0% |
| 6 | UI + Boosters | 0% |
