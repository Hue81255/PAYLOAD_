# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PAYLOAD_** is a Unity 2022.3.62f3 strategy game where the player controls a virus/malware attempting to infect 9 city district regions while evading an AI white hacker opponent.

## Build & Development

This is a Unity project — open it via Unity Hub with Unity **2022.3.62f3 (LTS)**. There are no separate build scripts; use Unity Editor's Build Settings (File → Build Settings) to build.

- **Play in Editor:** Open `Assets/Scenes/main.unity` (or `Assets/Scene/Main.unity`) and press Play.
- **Tests:** Unity Test Framework is included (`com.unity.test-framework`). Run via Unity Editor → Window → General → Test Runner.
- **IDE:** Rider, VS Code, and Visual Studio are all configured in `Packages/manifest.json`.

## Architecture

### Singleton Managers

The core systems use `Instance` singletons. Key ones:
- `PlayerStats` — player's inf/comp/stealth stats and coin currency
- `UIManager` — all HUD updates go through here
- `RegionDataLoader` — loads `Resources/Data.json` at startup; use `GetRegionById(id)` for lookups

### Loose Coupling via GlobalEventManager

Systems communicate through `GlobalEventManager` (static events) rather than direct references:
- `OnHackSuccess` — fired after a successful region infection
- `OnTimeChanged` — fired by `TimeManager` each game hour tick
- `OnBackdoorActive` — triggers `BackdoorManager` to reduce cure progress by 20%

### Game Flow

```
InputHandler (click) → InfectionEngine.AttemptHack()
    → checks PlayerStats vs RegionData.minStats
    → on success: PlayerStats.AddCoins(), GlobalEventManager.OnHackSuccess
    → GameManager tracks infected count (win at 9/9)

CureManager (background coroutine)
    → starts when infection hits 35% threshold
    → drives cure progress bar in UIManager
    → triggers phase events at 30% / 60% / 90%
    → at 100%: GameManager.GameOver()

WhiteHackerManager (FSM coroutine)
    → Idle → Scanning (every 10s) → Curing (~15s per region) → Alert (at 60%+ cure)
    → Alert state doubles curing speed, deducts 50 coins per region cured
```

### Region Data

All 9 regions are defined in `Assets/Resources/Data.json` (Daegu city districts). Each region has `minStats` thresholds (inf/comp/stealth) the player must meet to infect it, and a `reward` in coins. `RegionController` holds the runtime state; `RegionDataLoader` owns the static definitions.

### EvolutionManager Upgrade System

Two upgrade axes:
1. **Mutation stages (1→4):** Cost 80/200/400 coins. Stage 3 requires 1+ institution captured OR 50% infection rate; Stage 4 requires 3+ institutions.
2. **Upgrade trees (Spread/Stealth/Destructive/Penetration):** Up to 4 levels each, costing 50/100/180/300 coins per level.

Mutations can trigger `RandomMutationEvent` which applies stat deltas with trade-offs.

### Time System

`TimeManager` runs a 24-hour cycle. Day (06:00–20:00) uses multiplier 1.0x; Night (20:00–06:00) uses 0.5x cure speed. `RegionController` also adjusts per-region stats by time: Business districts get 1.5x infection speed at night with 0.8x stealth.

### InfectionEngine Attack Features

Six `AttackFeature` types modify the hack attempt outcome (e.g., `DataTheft` grants +50% coin rewards, `Ransomware` forces max infection but triggers +300% detection risk). These are applied before probability calculation in `AttemptHack()`.

## Key Constants (hardcoded in scripts)

| Value | Location | Meaning |
|---|---|---|
| 35% infection | `CureManager` | Detection warning threshold |
| 30/60/90% cure | `CureManager` | Phase event triggers |
| 0.3%/frame + 0.05%/region | `CureManager` | Base cure speed |
| 10s / 15s | `WhiteHackerManager` | Scan interval / cure duration |
| -10 stealth | `CureManager` (90% event) | Forensic monitoring penalty |
