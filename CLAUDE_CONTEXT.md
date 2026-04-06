# GRAVITY SORT — Project Context for Claude Code
# ⚠️ READ THIS FILE AT THE START OF EVERY SESSION ⚠️
# Last updated: Phase 3 Complete

---

## QUICK RESUME

**Current Phase:** Phase 4 — Levels + Win/Lose Flow
**What's done:** Grid, blocks, input, selection, pour, settle, match detection, chain reactions, combo counter, score system, level validator, block drops, drop animation, next block preview, game over detection, near-full column warning, win condition check
**What's next:** 30 level designs, level loading flow, win/lose popups, level progression and unlock system, level select screen
**Main GDD:** Read `GravitySort_GDD_Complete.md` in project root for full game design

---

## PROJECT SETUP

- **Engine:** Unity 2022.3 LTS
- **Template:** 3D Core (converted to URP manually)
- **Render Pipeline:** URP with bloom enabled (intensity ~0.8, threshold ~0.7)
- **Orientation:** Portrait (1080 × 1920)
- **Key Packages:** DOTween (free), TextMeshPro, URP, Newtonsoft JSON
- **MCP:** Coplay MCP connected
- **Domain Reload:** Disabled for fast Play Mode

---

## FOLDER STRUCTURE

```
Assets/_GravitySort/
├── Scripts/
│   ├── Core/            — GameManager (not yet), AudioManager (not yet)
│   ├── Gameplay/
│   │   ├── Block.cs              ✅ DONE
│   │   ├── GridManager.cs        ✅ DONE
│   │   ├── InputHandler.cs       ✅ DONE
│   │   ├── GameplayController.cs ✅ DONE (pour flow + chain + BlockDropper integration + win detection)
│   │   ├── PourAnimator.cs       ✅ DONE (DOJump arc, stagger, landing bounce)
│   │   ├── MatchChecker.cs       ✅ DONE (scans all columns, returns List<MatchResult>)
│   │   ├── ChainReactionHandler.cs ✅ DONE (async chain loop, OnBlocksCleared, OnChainComplete)
│   │   ├── ScoreManager.cs       ✅ DONE (base points × combo multiplier, OnScoreChanged event)
│   │   ├── BlockDropper.cs       ✅ DONE (timer drops, overflow detection, pause/resume, OnBlockDropped/OnColumnOverflow/OnAllDropsComplete)
│   │   └── BoosterManager.cs     ❌ TODO (Phase 6)
│   ├── UI/
│   │   ├── NextBlockPreview.cs   ✅ DONE (3 upcoming drops, column indicator lines, auto-hides on exhausted)
│   │   └── ColumnWarningVisual.cs ✅ DONE (red pulse overlay at dangerRow 7, DOTween yoyo loop)
│   └── Data/
│       ├── GameConfig.cs         ✅ DONE
│       ├── LevelData.cs          ✅ DONE
│       └── PlayerProgress.cs     ❌ TODO (Phase 4)
├── Prefabs/
│   ├── Block.prefab              ✅ DONE
│   └── Particles/               — Empty (Day 4-5)
├── ScriptableObjects/
│   ├── GameConfig.asset          ✅ DONE
│   └── Levels/
│       ├── Level_Test.asset      ✅ DONE (10 starting blocks, 20 drops, 12R/9B/9G totals, ClearAll)
│       └── Level_Test_Chain.asset ✅ DONE (9 blocks, single pour triggers 2-step chain)
├── Sprites/UI/                  — Empty
├── Audio/SFX/                   — Empty
├── Audio/Music/                 — Empty
├── Materials/                   — Empty
├── Shaders/                     — Empty
├── Settings/                    — URP pipeline assets here
└── Scenes/
    └── SampleScene.unity         ✅ Main scene (has GridManager, InputHandler, etc.)
```

---

## WHAT'S BEEN BUILT (Detailed)

### GameConfig.cs (ScriptableObject)
- 5 block colors: #FF4D6A (red), #4DA6FF (blue), #5BDB6E (green), #FFD84D (yellow), #B44DFF (purple)
- Animation timings: pourArcHeight 1.5, pourBlockDuration 0.2, pourStagger 0.08, dropDuration 0.4, clearDuration 0.3, settleDuration 0.2
- Selection: pulseScale 1.08, pulseDuration 0.4
- Gameplay: matchThreshold 3, previewCount 3
- Boosters: freeUndo 2, freeFreeze 1, freeBomb 1, freezeDuration 10
- Economy: undoGemCost 5, freezeGemCost 8, bombGemCost 10, continueGemCost 5

### LevelData.cs (ScriptableObject)
- Fields: levelNumber, columnCount, maxHeight, colorCount, dropInterval, winCondition (enum: ClearAll/ReduceBelow/Survive), winTarget, startingBlocks[], dropSequence[], maxDrops
- StartingBlock struct: column (int), colorIndex (int)
- DropEntry struct: column (int), colorIndex (int)

### Block.cs
- Properties: colorIndex, SpriteRenderer reference
- Methods: SetColor(index, config), SetSelected(bool), PlayClearAnimation(callback), PlayDropAnimation(target, duration, callback), PlayPourArc(target, arcHeight, duration, callback), Init(), ResetBlock()
- ResetBlock kills tweens, resets scale to cellSize-based scale, resets alpha, deactivates
- Uses DOTween for all animations

### GridManager.cs
- Data: List<int>[] columnData, Block[,] blockVisuals
- Object pool of Block instances
- Calculates cell size dynamically based on screen width / columnCount
- Blocks scaled to cellSize * 0.85 (gap between blocks)
- Key methods: InitGrid(), GetWorldPosition(col, row), SpawnBlockAt(), SpawnInitialBlocks(), GetTopColorGroup(), GetColumnHeight(), IsColumnFull(), IsColumnEmpty(), GetTopColor(), GetColorAt(), GetBlockVisual(), ColumnCount, AddBlockToColumn(), RemoveBlocksFromTop(), RemoveBlocksFromTopDataOnly(), AddBlockToColumnData(), SetBlockVisual(), RemoveBlocksAtRange() ✅ DONE, RefreshVisuals(), SettleColumn() ✅ DONE, SettleAllColumns() ✅ DONE
- Public CellSize property for other scripts to reference

### InputHandler.cs
- Detects mouse click / touch
- Converts screen → world position
- Determines column from world X position using grid boundaries
- Event: OnColumnTapped(int columnIndex)
- Public inputEnabled bool to block input during animations

### GameplayController.cs ✅ DONE
- References: GridManager, InputHandler, PourAnimator, ChainReactionHandler, BlockDropper
- Pour flow: PauseDrops + lock input → grab Block[] refs → ExecutePour (data-only) → AnimatePour → SettleColumn → StartChainCheck()
- HandleChainComplete: re-enables input + ResumeDrops + CheckWinCondition (single exit point for all animation sequences)
- HandleBlockDropped: calls StartChainCheck() (input already locked by ExecuteDrop)
- HandleColumnOverflow: StopDrops + lock input + Debug.Log GAME OVER
- CheckWinCondition: reads LevelData.winCondition — ClearAll (all empty + allDropsExhausted), ReduceBelow (total blocks < winTarget), Survive (skipped)
- SetLevel(LevelData): arms win detection, resets allDropsExhausted flag

### MatchChecker.cs ✅ DONE
- CheckAllColumns() → List<MatchResult> — scans every column bottom-to-top for contiguous groups >= matchThreshold
- MatchResult struct: column, startRow, count, colorIndex

### ChainReactionHandler.cs ✅ DONE
- StartChainCheck() → async loop: CheckAllColumns → clear animations → RemoveBlocksAtRange → SettleAllColumns → recheck
- Matches sorted top-down per column before removal (preserves lower row indices)
- Block refs collected BEFORE data mutation; PlayClearAnimation first, then RemoveBlocksAtRange
- Events: OnBlocksCleared(int blocksCleared, int comboStep), OnChainComplete(int finalCombo)

### ScoreManager.cs ✅ DONE
- Subscribes to ChainReactionHandler events
- Base points: 3 blocks=100, 4=200, 5+=350
- Combo multiplier: step 1=×1, step 2=×2, step 3=×3, step 4+=×5
- Events: OnScoreChanged(int newScore)
- CurrentScore and CurrentCombo public properties

### LevelValidator.cs (Editor) ✅ DONE
- Menu: GravitySort → Validate All Levels
- ValidateLevel(LevelData, threshold): checks each color count divisible by matchThreshold
- Loads real matchThreshold from GameConfig.asset; warns per offending color

### BlockDropper.cs ✅ DONE
- SetLevel(LevelData): arms timer + sequence, sets isActive=true
- Update(): gates on isActive AND inputHandler.inputEnabled (two separate concerns)
- ExecuteDrop(): locks input → AddBlockToColumn (data+visual) → teleport block above grid (MaxRows+2) → PlayDropAnimation → fires OnBlockDropped
- PauseDrops()/ResumeDrops(): external pause control (pour animations, Freeze booster)
- StopDrops(): exhausts sequence, used by game over and win
- GetUpcomingDrops(int): returns slice from currentDropIndex without advancing
- Events: OnBlockDropped(int column), OnColumnOverflow(int column), OnAllDropsComplete

### NextBlockPreview.cs ✅ DONE
- Shows next config.previewCount (3) upcoming drops above the grid
- Each slot: colored square (50% block size) + thin column indicator line (same sprite, stretched)
- Y position: GetWorldPosition(0, MaxRows).y + 0.6 × cellSize (above game-over line)
- Refreshes on OnBlockDropped; hides all on OnAllDropsComplete
- Public Refresh() for initial display before first drop fires

### ColumnWarningVisual.cs ✅ DONE
- Polls GetDangerColumns(dangerRow=7) every frame (cheap O(n))
- Per-column state tracking: only starts/stops DOTween on transitions (no per-frame churn)
- Red overlay sprite (sortingOrder=-1, behind all blocks), alpha pulses 0.1↔0.3 via DOTween Yoyo loop

### Scene Setup
- Main Camera: orthographic, background #1A1A2E, post-processing enabled
- Global Volume with Bloom (intensity ~0.8, threshold ~0.7)
- GridManager GameObject with script + GameConfig + Block prefab assigned
- InputHandler on Manager
- Manager GameObject: InputHandler, GameplayController, PourAnimator, ChainReactionHandler, ScoreManager, BlockDropper, ColumnWarningVisual, NextBlockPreview — all cross-references wired
- TestBootstrap: calls InitGrid → SpawnInitialBlocks → blockDropper.SetLevel → gameplayController.SetLevel → nextBlockPreview.Refresh()

---

## KNOWN ISSUES / DECISIONS MADE

1. **Block scale fix:** Initial blocks were tiny (0.9 fixed scale). Fixed by scaling to cellSize * 0.85 dynamically from GridManager
2. **3D Core template:** Started with 3D Core, manually installed URP + converted. Working fine
3. **Grid position:** Grid sits at bottom of screen — may need to shift up later to make room for booster buttons. Not a blocker now

---

## DEVELOPMENT PHASES REMAINING

### ✅ Phase 1 — COMPLETE
- Pour validation, execution, animation (PourAnimator DOJump + stagger + landing bounce)
- Gravity settle after pour (SettleColumn / SettleAllColumns with OutBounce)
- Input locked during animations, re-enabled on complete

### ✅ Phase 2 — COMPLETE
- MatchChecker.cs — contiguous group detection, MatchResult struct
- ChainReactionHandler.cs — async clear → settle → recheck loop, combo events
- ScoreManager.cs — base points × combo multiplier, OnScoreChanged event
- RemoveBlocksAtRange in GridManager — mid-column removal with visual slot shifting
- Level design rule: color counts must be divisible by matchThreshold
- LevelValidator.cs editor tool — GravitySort → Validate All Levels menu

### ✅ Phase 3 — COMPLETE
- BlockDropper.cs — timer drops, overflow detection, pause/resume, events
- Drop animation (fall from MaxRows+2, OutBounce, PlayDropAnimation)
- NextBlockPreview UI (3 upcoming drops, column indicator lines)
- Game over detection (OnColumnOverflow → StopDrops + lock input + Debug.Log)
- Near-full column warning (ColumnWarningVisual, red pulse at dangerRow=7)
- Win condition check in GameplayController (ClearAll / ReduceBelow / Survive)
- TestBootstrap wired to call SetLevel on BlockDropper + GameplayController

### Phase 4 — Levels + Win/Lose Flow (CURRENT)
- 30 LevelData ScriptableObjects (level designs)
- LevelManager.cs — level loading, progression, unlock tracking
- Win popup (level complete overlay, star rating, next level button)
- Lose popup (game over overlay, retry / continue with gems)
- Level select screen
- PlayerProgress.cs — save/load unlock state

### Phase 5 — Days 10-12: Polish
- Particle effects, screen shake, haptics
- Sound effects + music
- Score rolling counter
- All animation refinement

### Phase 6 — Days 13-14: UI + Boosters
- Main menu, HUD, pause
- Booster system (Undo, Freeze, Bomb)
- Basic currency display
- Fail-level continue offer

---

## ARCHITECTURE RULES (Always Follow These)

1. **Data and visuals are separate.** GridManager.columnData is the source of truth. Never read game state from block positions — always from data arrays.
2. **Animation locks input.** Set InputHandler.inputEnabled = false during any animation. Re-enable only when returning to PLAYING state.
3. **Event-driven communication.** Use C# events: OnPourComplete, OnMatchCleared, OnSettleComplete, OnBlockDropped, OnColumnOverflow.
4. **Object pooling for blocks.** Never Instantiate/Destroy during gameplay. Use ResetBlock() to return to pool.
5. **ScriptableObject for ALL config.** No magic numbers. All values come from GameConfig.
6. **DOTween for ALL animations.** Use DOTween with proper easing. Always .SetAutoKill(true) and kill existing tweens before starting new ones on the same transform.
7. **Pre-determined drop sequences.** Drops come from LevelData.dropSequence, not Random. Ensures solvability.
8. **LEVEL DESIGN RULE — Color Count Divisibility:** For ClearAll levels, the total count of each color (startingBlocks + all blocks in dropSequence) must be divisible by matchThreshold (default 3). This guarantees the level is mathematically clearable. Every level must be validated against this rule before shipping. Example: 6 Reds, 6 Blues, 6 Greens ✓ — 7 Reds ✗.

---

## HOW TO START A NEW SESSION

Paste this to Claude Code at session start:

```
Read the files CLAUDE_CONTEXT.md and GravitySort_GDD_Complete.md in my project. 
CLAUDE_CONTEXT.md has the current project state, what's built, and what's next. 
GravitySort_GDD_Complete.md has the full game design document. 
Use both as reference for all work. Confirm you've read both and tell me what phase we're in.
```
