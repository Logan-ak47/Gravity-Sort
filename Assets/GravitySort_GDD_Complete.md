# GRAVITY SORT — Complete MVP Game Design Document
# Version 1.0 | Unity 2D (URP) | Solo Dev

---

## PROJECT OVERVIEW

**Game:** Gravity Sort
**Genre:** Hybrid-Casual Puzzle
**Platform:** iOS + Android (Mobile, Portrait only)
**Engine:** Unity 2022.3 LTS, 2D URP
**Target:** Low DAU / High ARPU
**MVP Scope:** 30 levels, core loop + basic meta + 3 boosters

**One-liner:** Color-sort puzzle meets slow-falling blocks — sort matching colors in columns while new blocks keep dropping from above.

**Core Fantasy:** The satisfying "click" of organizing chaos under gentle pressure.

**Reference Games:** Water Sort Puzzle (sort mechanic), Tetris (gravity pressure), Coin Sort (hybrid-casual monetization), Color Block Jam (merged mechanics).

---

## 1. CORE MECHANICS

### 1.1 The Board

- Grid: 6 columns × 8 rows (configurable per level)
- Blocks: Colored rounded squares that stack vertically in columns
- Starting state: Each level begins with pre-placed blocks defined in LevelData
- Colors: 3 colors (levels 1-10), 4 colors (levels 11-25), 5 colors (levels 26+)

```
NEXT BLOCK PREVIEW (3 upcoming blocks shown)
        ↓   ↓   ↓   ↓   ↓   ↓
      ┌───┬───┬───┬───┬───┬───┐
  8   │   │   │   │   │   │   │  ← GAME OVER LINE (any block here = lose)
      ├───┼───┼───┼───┼───┼───┤
  7   │   │   │   │   │   │   │  ← DANGER ZONE (column pulses red here)
      ├───┼───┼───┼───┼───┼───┤
  6   │   │   │   │   │   │   │
      ├───┼───┼───┼───┼───┼───┤
  5   │   │ G │   │   │   │ R │
      ├───┼───┼───┼───┼───┼───┤
  4   │ R │ G │   │ B │   │ R │
      ├───┼───┼───┼───┼───┼───┤
  3   │ B │ R │ G │ B │   │ G │
      ├───┼───┼───┼───┼───┼───┤
  2   │ R │ B │ R │ G │ B │ B │
      ├───┼───┼───┼───┼───┼───┤
  1   │ R │ B │ R │ G │ B │ G │
      └───┴───┴───┴───┴───┴───┘
       C1   C2  C3   C4  C5  C6
```

### 1.2 Player Actions

**ONE input gesture only: Tap**

- Tap source column → top contiguous same-color group highlights
- Tap destination column → pour if valid
- Tap same column again → deselect
- Tap empty column as source → nothing happens

No swipes, no drags, no holds. One-tap simplicity is critical for casual accessibility.

### 1.3 Pour Rules

A pour from Column A to Column B is valid ONLY when:
1. Column A is not empty
2. Column B is not full (block count < maxRows)
3. The top color of Column A matches the top color of Column B, OR Column B is empty
4. There is enough space in Column B for at least 1 block from the pour group

When pouring:
- The entire contiguous same-color group from the top of Column A moves
- If Column B doesn't have room for all blocks, only as many as fit are moved
- After pour completes, gravity settles remaining blocks in Column A

### 1.4 Block Drop System (Gravity)

New colored blocks drop from above at timed intervals:
- A block drops into a specific column every X seconds (defined per level)
- The drop sequence is PRE-DETERMINED per level (seeded, not random) to ensure solvability
- Player can see a "NEXT" preview showing the next 3 upcoming blocks (color + target column)
- Blocks fall with a smooth animation, not instant
- If target column is full (height = 8), GAME OVER

Drop interval by difficulty:

| Level Range | Drop Interval | Colors |
|-------------|--------------|--------|
| 1-5         | 4.0s         | 3      |
| 6-10        | 3.5s         | 3      |
| 11-15       | 3.0s         | 4      |
| 16-20       | 2.7s         | 4      |
| 21-25       | 2.4s         | 4      |
| 26-30       | 2.2s         | 5      |

### 1.5 Match & Clear

When 3 or more same-colored blocks are contiguous (stacked vertically) in a single column, they AUTO-CLEAR:
- Clear triggers AFTER: every pour landing, every gravity settle, every block drop landing
- Cleared blocks play a burst animation then are removed
- Remaining blocks above settle down (gravity)
- After settle, re-check ALL columns for new matches (CHAIN REACTION)
- Chain reactions continue until no more matches exist
- Each step in a chain increments a combo counter

Match threshold: 3 blocks (upgradeable to 2 via power-up in future).

**IMPORTANT:** Match checking runs ONLY on columns, not rows. Horizontal matches do NOT count. This keeps the mechanic simple and consistent with the "sort into columns" mental model.

### 1.6 Win / Lose Conditions

**WIN:**
- "ClearAll" mode: Clear every block from the board (no blocks dropping AND board empty)
- "ReduceBelow" mode: Reduce total block count below a target number
- "Survive" mode: Survive X number of drops without game over (future mode)

**LOSE:**
- A new block tries to drop into a column that is already at max height (row 8)

**Near-lose warning:**
- Any column at height 7 → that column background pulses red
- Any column at height 6 → subtle yellow tint (optional)

### 1.7 Scoring

| Action | Points |
|--------|--------|
| 3-block clear | 100 |
| 4-block clear | 200 |
| 5+ block clear | 350 |
| Chain reaction ×2 | Points × 2 multiplier |
| Chain reaction ×3 | Points × 3 multiplier |
| Chain reaction ×4+ | Points × 5 multiplier |
| Level complete bonus | 500 × level number |
| Perfect clear (0 blocks remain) | +1000 bonus |

---

## 2. LEVEL DESIGN

### 2.1 Level Data Structure

Each level is defined by a ScriptableObject:

```csharp
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

public enum WinConditionType { ClearAll, ReduceBelow, Survive }
```

### 2.2 Level Progression (30 Levels)

**Tutorial Zone (Levels 1-3): Teach the mechanics**
- Level 1: 3 columns, 2 colors, NO drops (pure sort tutorial)
- Level 2: 4 columns, 2 colors, very slow drops (6s interval)
- Level 3: 5 columns, 3 colors, slow drops (5s)

**Easy Zone (Levels 4-10): Build confidence**
- 6 columns, 3 colors, 4.0s drops
- Starting blocks: few and mostly sorted
- Goal: teach chain reactions by creating boards where chains are likely

**Medium Zone (Levels 11-20): Create healthy frustration**
- 6 columns, 3-4 colors, 3.0-3.5s drops
- Starting blocks: more mixed, some deliberately awkward placements
- ~20-30% of players should fail on first attempt → drives retry + IAP

**Hard Zone (Levels 21-30): Serious challenge**
- 6 columns, 4-5 colors, 2.2-2.5s drops
- Starting blocks: heavily mixed, some near-full columns
- ~50% failure rate on first attempt
- These levels are designed to be ALMOST completable, creating "I was so close" feeling

### 2.3 Difficulty Tuning Levers

| Lever | Easier ← | → Harder |
|-------|----------|----------|
| Drop interval | 4.0s+ | 1.8s |
| Number of colors | 2-3 | 4-5 |
| Starting blocks | Few, pre-sorted | Many, mixed |
| Empty columns at start | 2-3 empty | 0-1 empty |
| Win condition | ReduceBelow(8) | ClearAll |
| Drop sequence | Gives helpful colors | Gives unhelpful colors |
| Total drops (maxDrops) | Fewer total | More total |

### 2.4 CRITICAL DESIGN RULE

The drop sequence MUST be pre-determined (not random). This ensures:
- Every level IS solvable with optimal play
- Playtesting is reproducible (same board state every time)
- Difficulty can be precisely tuned per level
- "Unfair" feeling is minimized — failure = player skill, not bad luck

---

## 3. BOOSTERS (3 Types for MVP)

Boosters are limited-use power-ups. Players get 2 free of each per level. Additional uses cost gems (hard currency).

### 3.1 UNDO
- Reverses the last pour action
- Restores source and destination columns to pre-pour state
- Does NOT reverse block drops or match clears
- Cannot be used if no pour has been made

### 3.2 FREEZE
- Pauses the block drop timer for 10 seconds
- Blocks already falling complete their animation
- Visual: timer icon appears with countdown, drop queue dims
- Gives breathing room on hard levels

### 3.3 BOMB
- Player taps BOMB button → then taps a column → removes ALL blocks of the TOP color in that column
- Cleared blocks play normal clear animation + particles
- Remaining blocks settle with gravity
- Match check runs after settle (can trigger chains)

### 3.4 Booster Economy

| Booster | Free per level | Gem cost for extra |
|---------|---------------|-------------------|
| UNDO    | 2             | 5 gems            |
| FREEZE  | 1             | 8 gems            |
| BOMB    | 1             | 10 gems           |

---

## 4. CURRENCY & ECONOMY (Basic MVP)

### 4.1 Currencies

**Coins (Soft Currency):**
- Earned from: level completion (100-500 per level), chain reactions, daily login
- Spent on: cosmetic themes (future), basic boosters

**Gems (Hard Currency):**
- Earned from: level milestones (every 5 levels = 10 gems), watching rewarded ads, achievements
- Spent on: extra boosters, continues after game over
- Purchasable via IAP (future)

### 4.2 Fail-Level Offer

When player gets GAME OVER:
1. Show "Game Over" screen with final score
2. Offer: "Continue? Remove top row blocks for 5 gems" (one-time per attempt)
3. If accepted: remove all blocks in row 7-8, resume play
4. If declined: return to level select
5. Also offer: "Watch ad to continue for free" (rewarded ad, once per level)

### 4.3 Rewarded Ad Placements

| Placement | Reward | Frequency |
|-----------|--------|-----------|
| Continue after game over | Free continue (1×) | Once per level attempt |
| Double level-complete coins | 2× coin reward | After every level win |
| Free booster refill | +1 of each booster | Every 3 levels |

---

## 5. VISUAL DESIGN

### 5.1 Art Style: "Clean Glow"

Minimalist flat design with glow effects on a dark background. Everything is colored geometric shapes — no illustrated art, no textures, no characters.

### 5.2 Color Palette

```
Background:        #1A1A2E (deep navy)
Grid lines:        #2A2A4A (subtle)
Danger zone:       #FF4D6A with 25% alpha
Game over line:    #FF4D6A with 15% alpha (dashed)

Block Colors (5 total, high contrast, colorblind-safe):
  Index 0 - Red:      #FF4D6A
  Index 1 - Blue:     #4DA6FF
  Index 2 - Green:    #5BDB6E
  Index 3 - Yellow:   #FFD84D
  Index 4 - Purple:   #B44DFF

UI text:           #FFFFFF
Score text:        #FFD84D (gold)
Combo text:        #FF4D6A → scales up and fades
Button primary:    #4DA6FF
Button secondary:  #2A2A4A with #4DA6FF border
```

### 5.3 Block Visual Spec

- Shape: Rounded square (cornerRadius ≈ 15% of block size)
- Fill: Flat solid color from palette above
- Inner shadow: Subtle (top-left highlight, bottom-right shadow) for depth
- Selected state: White outline glow (2px) + scale 1.05× + gentle pulse
- Clearing state: Scale 1.2× → burst into color-matched particles → fade out
- Size: Calculated dynamically = (screenWidth - totalPadding) / columnCount

### 5.4 Animation Spec

| Event | Animation | Duration | Easing |
|-------|-----------|----------|--------|
| Block drop (new block from above) | Fall from top of screen to column position | 0.4s | OutBounce (subtle) |
| Pour (source → destination) | Arc trajectory, blocks fly one by one, staggered | 0.2s per block, 0.08s stagger | InOutQuad |
| Pour landing | Overshoot down 5% → bounce back | 0.15s | OutBack |
| Match clear | Scale 1.0→1.2 → particle burst → remove | 0.3s | OutQuad |
| Chain reaction | Screen shake (2px, 0.15s) + combo text popup | 0.2s | — |
| Gravity settle | Blocks fall to new position | 0.2s | OutBounce (very subtle) |
| Column near-full (row 7) | Column bg pulses red | 1.0s loop | InOutSine |
| Game over | All blocks shatter top-down → screen darkens | 1.0s | — |
| Level complete | Blocks fly to center → star burst particle | 0.8s | — |
| Score counter | Rolling number increment | 0.3s | OutQuad |
| Combo text | "×2!" scales up from 0→1 + floats up + fades | 0.6s | OutBack |
| Block selection pulse | Scale 1.0→1.08→1.0 continuous | 0.4s per cycle | InOutSine |

### 5.5 UI Layout (Portrait 9:16)

```
┌──────────────────────────────┐
│  ⚙️      LEVEL 12       💎 450  │  Top bar
├──────────────────────────────┤
│                              │
│  NEXT: [R→C3] [B→C1] [G→C5] │  Drop preview (3 upcoming)
│                              │
│  ┌──────────────────────┐    │
│  │ - - - - - - - - - -  │    │  Game over line (dashed)
│  │                      │    │
│  │                      │    │
│  │   GAME GRID          │    │
│  │   (6 cols × 8 rows)  │    │
│  │                      │    │
│  │                      │    │
│  │                      │    │
│  └──────────────────────┘    │
│                              │
│  SCORE: 2,450   COMBO: ×3   │  Score bar
│                              │
│  ┌──────┐ ┌──────┐ ┌──────┐ │
│  │ UNDO │ │FREEZE│ │ BOMB │ │  Booster buttons
│  │  ×2  │ │  ×1  │ │  ×1  │ │
│  └──────┘ └──────┘ └──────┘ │
│                              │
│  ▶ PAUSE                    │
└──────────────────────────────┘
```

### 5.6 Sound Design

| Event | Sound | Character |
|-------|-------|-----------|
| Tap column (select) | Soft "tick" | Short, clean |
| Pour start | Ascending "swoosh" | Airy, light |
| Block lands | Light "click/plop" | Satisfying thud |
| 3-match clear | Bright ascending chime | Rewarding |
| Chain ×2 | Higher pitch chime | More rewarding |
| Chain ×3+ | Triumphant chord | Celebration |
| New block drops in | Muted "plop" | Non-intrusive |
| Danger warning | Low pulse hum | Tension |
| Game over | Descending notes + glass break | Disappointing but not harsh |
| Level complete | 2-3 second celebration jingle | Joyful |
| Booster used | Magic "shimmer" | Empowering |
| Background music | Lo-fi chill loop, non-distracting | Calm, ambient |

---

## 6. TECHNICAL ARCHITECTURE

### 6.1 Project Structure

```
Assets/
├── _GravitySort/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs          — Game state machine (singleton)
│   │   │   ├── LevelManager.cs         — Level loading, progression, save/load
│   │   │   ├── AudioManager.cs         — Sound effect + music management
│   │   │   └── CurrencyManager.cs      — Coins + gems tracking
│   │   ├── Gameplay/
│   │   │   ├── GridManager.cs          — Column/block data, world positioning
│   │   │   ├── Block.cs               — Single block component, visuals, animations
│   │   │   ├── Column.cs              — Column container, height tracking
│   │   │   ├── InputHandler.cs        — Tap detection → column index
│   │   │   ├── GameplayController.cs  — Selection, pour, game flow
│   │   │   ├── PourAnimator.cs        — Arc animation for moving blocks
│   │   │   ├── MatchChecker.cs        — Detects 3+ vertical matches
│   │   │   ├── ChainReactionHandler.cs — Manages cascading clears + combos
│   │   │   ├── BlockDropper.cs        — Timed block drops from above
│   │   │   └── BoosterManager.cs      — Undo, Freeze, Bomb logic
│   │   ├── UI/
│   │   │   ├── HUD.cs                 — In-game score, combo, gem display
│   │   │   ├── NextBlockPreview.cs    — Shows upcoming 3 drops
│   │   │   ├── ComboPopup.cs          — Animated combo text
│   │   │   ├── LevelCompletePopup.cs  — Win screen
│   │   │   ├── GameOverPopup.cs       — Lose screen + continue offer
│   │   │   ├── MainMenu.cs           — Play button, settings
│   │   │   ├── LevelSelectUI.cs      — Level grid with lock/unlock
│   │   │   ├── PausePopup.cs         — Pause overlay
│   │   │   └── BoosterUI.cs          — Booster buttons + count display
│   │   └── Data/
│   │       ├── LevelData.cs           — ScriptableObject definition
│   │       ├── GameConfig.cs          — Global config (colors, sizes, timing)
│   │       └── PlayerProgress.cs      — Save data (unlocked levels, currencies)
│   ├── Prefabs/
│   │   ├── Block.prefab
│   │   ├── Column.prefab
│   │   └── Particles/
│   │       ├── MatchClearFX.prefab
│   │       ├── ChainFX.prefab
│   │       └── LevelCompleteFX.prefab
│   ├── ScriptableObjects/
│   │   ├── GameConfig.asset           — Singleton config
│   │   └── Levels/
│   │       ├── Level_01.asset ... Level_30.asset
│   ├── Sprites/
│   │   ├── block_rounded.png          — White rounded square (tinted via code)
│   │   └── UI/
│   ├── Audio/
│   │   ├── SFX/
│   │   └── Music/
│   ├── Materials/
│   │   └── BlockGlow.mat              — Additive material for selection glow
│   ├── Shaders/
│   │   └── RoundedRect.shader         — Optional: shader-based rounded corners
│   └── Scenes/
│       ├── Boot.unity
│       ├── MainMenu.unity
│       └── Game.unity
```

### 6.2 Game State Machine (GameManager.cs)

```
States:
  BOOT          → Initialize managers, load player progress
  MAIN_MENU     → Show main menu UI
  LOADING_LEVEL → Read LevelData, spawn grid, reset state
  PLAYING       → Player can interact, drops are active
  POURING       → Pour animation in progress (INPUT BLOCKED)
  CLEARING      → Match clear animation playing (INPUT BLOCKED)
  CHAIN_CHECK   → After clear, gravity settle → re-check matches
  LEVEL_COMPLETE → Win popup, score summary, rewards
  GAME_OVER     → Lose popup, continue offer
  PAUSED        → Pause overlay, drop timer paused

Transitions:
  BOOT → MAIN_MENU
  MAIN_MENU → LOADING_LEVEL (player taps Play)
  LOADING_LEVEL → PLAYING
  PLAYING → POURING (valid pour initiated)
  POURING → CLEARING (pour complete, match found)
  POURING → PLAYING (pour complete, no match)
  CLEARING → CHAIN_CHECK (clear animation done)
  CHAIN_CHECK → CLEARING (new match found after settle)
  CHAIN_CHECK → PLAYING (no new matches)
  PLAYING → GAME_OVER (column overflow)
  PLAYING → LEVEL_COMPLETE (win condition met)
  PLAYING → PAUSED (pause button)
  PAUSED → PLAYING (resume)
  GAME_OVER → LOADING_LEVEL (retry) or MAIN_MENU (quit)
  LEVEL_COMPLETE → LOADING_LEVEL (next level) or MAIN_MENU
```

### 6.3 GameConfig ScriptableObject

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "GravitySort/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Block Colors")]
    public Color[] blockColors = new Color[] {
        new Color(1f, 0.3f, 0.42f),    // #FF4D6A Red
        new Color(0.3f, 0.65f, 1f),     // #4DA6FF Blue
        new Color(0.36f, 0.86f, 0.43f), // #5BDB6E Green
        new Color(1f, 0.85f, 0.3f),     // #FFD84D Yellow
        new Color(0.7f, 0.3f, 1f)       // #B44DFF Purple
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
```

### 6.4 Key Architecture Rules

1. **Data and visuals are separate.** GridManager holds the data (arrays of ints). Block MonoBehaviours are visual representations. Never read game state from visual positions — always from data.

2. **Animation locks input.** During ANY animation (pour, clear, settle, drop), the GameManager state blocks input. GameplayController only processes taps during PLAYING state.

3. **Event-driven communication.** Use C# events or UnityEvents for decoupled communication:
   - `OnPourComplete` → triggers match check
   - `OnMatchCleared` → triggers gravity settle + combo update
   - `OnSettleComplete` → triggers re-check for chains
   - `OnBlockDropped` → triggers match check
   - `OnColumnOverflow` → triggers game over

4. **Object pooling for blocks.** Don't Instantiate/Destroy blocks repeatedly. Create a pool of ~60 Block objects at level start. Activate/deactivate as needed.

5. **ScriptableObject for ALL config.** No magic numbers in code. All timing, colors, sizes, costs go in GameConfig. This lets you tune without recompiling.

6. **DOTween for ALL animations.** Use DOTween (free version) for every tween: movement, scaling, fading, color changes. This gives consistent easing and the ability to sequence/chain animations.

### 6.5 Critical Implementation Details

**Block world positioning:**
```
blockSize = (screenWorldWidth - padding) / columnCount
x = gridLeftEdge + (column * blockSize) + (blockSize / 2)
y = gridBottom + (row * blockSize) + (blockSize / 2)
```

**Pour arc trajectory:**
Use DOTween DOPath with 3 points: start position → peak (midpoint X, startY + arcHeight) → end position. OR use DOJump for simpler arc.

**Match check algorithm:**
```
For each column:
  Scan bottom to top
  Track current color and consecutive count
  When color changes or column ends:
    If count >= matchThreshold:
      Mark those blocks for clearing
  Reset count
```

**Chain reaction loop:**
```
1. Clear marked blocks (animate)
2. Wait for clear animation
3. Settle all columns (gravity, animate)
4. Wait for settle animation
5. Run match check on ALL columns
6. If new matches found → go to step 1 (increment combo)
7. If no matches → return to PLAYING state
```

---

## 7. DEVELOPMENT PHASES

### Phase 1: Core Grid + Sort (Days 1-3)
- Unity project setup (2D URP, portrait)
- GridManager: data structure + world positioning
- Block prefab: colored rounded square with SpriteRenderer
- Tap input → column detection
- Selection state: highlight top color group
- Pour logic: validation + data transfer
- Pour animation: arc trajectory with DOTween
- Gravity settle after pour

**Phase 1 success test:** Pouring blocks between columns feels snappy and satisfying.

### Phase 2: Match Detection + Chain Reactions (Days 4-5)
- MatchChecker: scan columns for 3+ vertical same-color
- Clear animation: scale up → particle burst → remove
- Gravity settle after clear
- Chain reaction loop (clear → settle → recheck)
- Combo counter + combo popup text
- Score system

**Phase 2 success test:** Chain reactions feel exciting and rewarding.

### Phase 3: Block Drop System (Days 6-7)
- BlockDropper: timer-based drops from LevelData.dropSequence
- Drop animation: fall from above with bounce landing
- NextBlockPreview UI: show upcoming 3 drops
- Game over check: column overflow detection
- Near-full warning: column pulse at row 7
- Match check after every drop landing

**Phase 3 success test:** Sort + drop together creates "relaxed urgency" — engaging, not stressful.

### Phase 4: Levels + Win/Lose Flow (Days 8-9)
- LevelData ScriptableObjects for 30 levels
- Level loading → grid spawn → gameplay → win/lose
- Win condition check after every clear
- Level complete popup with score
- Game over popup with retry
- Level progression (unlock next level on complete)
- Basic level select screen

**Phase 4 success test:** 30 levels with a satisfying difficulty curve.

### Phase 5: Game Feel Polish (Days 10-12)
- Particle effects for all events
- Screen shake on chain reactions
- Haptic feedback (Handheld.Vibrate on mobile)
- Score rolling counter animation
- All sound effects
- Background music loop
- Block bounce on landing refinement
- Danger zone visual polish

**Phase 5 success test:** Recording looks like something from the App Store.

### Phase 6: UI + Boosters + Menus (Days 13-14)
- Main menu with Play button
- HUD: score, combo, level, gems
- Pause popup
- Booster buttons (Undo, Freeze, Bomb) — functional
- Booster use count + gem cost
- Basic currency display
- Fail-level continue offer (5 gems or ad)
- Settings: sound/music/haptics toggles

**Phase 6 success test:** Complete playable game from app open to level 30.

---

## 8. SAVE DATA

### PlayerProgress (saved to PlayerPrefs or JSON file)

```csharp
[System.Serializable]
public class PlayerProgress
{
    public int highestLevelUnlocked = 1;
    public int coins = 0;
    public int gems = 50;  // starting gems
    public int[] levelStars;  // 0-3 stars per level
    public int[] levelHighScores;
    public bool soundEnabled = true;
    public bool musicEnabled = true;
    public bool hapticsEnabled = true;
}
```

For MVP, use PlayerPrefs (simple key-value). Migrate to JSON file if data grows.

---

## 9. FUTURE SCOPE (Post-MVP, Only If Core Loop Validates)

These features are NOT in the MVP but the architecture should not prevent them:

- **New block types:** Bomb block (clears adjacent), Frozen block (requires 2 pours to move), Wildcard block (matches any color), Stone block (cannot be moved, must be cleared around)
- **Themes/skins:** Color palette swaps for blocks and background (cosmetic IAP)
- **Season pass:** Free + premium reward tracks over 4-week seasons
- **Daily challenge:** One unique level per day with leaderboard
- **Weekly event:** Special rules (e.g., "only 2 colors but 2-match threshold")
- **Collection system:** Complete level sets to earn collectible items
- **Social:** Add friends, compare scores
- **Ad removal IAP:** $2.99 one-time purchase

---

## 10. PIVOT CRITERIA

### KEEP GOING if (after full MVP):
- You play your own game voluntarily for fun
- 3+ out of 5 testers say "I'd play this on my phone"
- Chain reactions feel genuinely satisfying
- Fail-level moments feel like "I was so close" not "that was unfair"

### PIVOT TO IDEA #2 (Hole & Thread) if:
- Core loop isn't fun by Day 7
- Drop mechanic feels annoying rather than exciting
- Playtesters are lukewarm ("it's fine")
- Sort + gravity doesn't feel meaningfully different from plain Water Sort
