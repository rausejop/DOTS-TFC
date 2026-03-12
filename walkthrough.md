# Deliverables Walkthrough

> **Generated:** 2026-03-13  
> **`DOTS_RTS_Prototype/`:** READONLY — context only, no modifications  

### Related Documents

| Document | Description |
|----------|-------------|
| [README.md](README.md) | Project overview, badges, and file manifest |
| [Game Design Document](Game_Design_Document.md) | Part A deliverable — full GDD |
| [Implementation Plan](implementation_plan.md) | Part B architecture alignment |
| [BUILD_INSTRUCTIONS.md](Vertical_Slice/BUILD_INSTRUCTIONS.md) | Android APK build guide |
| [Task Checklist](task.md) | Deliverables tracking |

---

## Part A: Game Design Document ✅

**File:** [Game_Design_Document.md](Game_Design_Document.md) (31 KB)

Covers all sections requested in the prompt:
- **Executive Summary** — Vision, Vertical Slice features table, core loop diagram
- **Gameplay Mechanics** — Side-scrolling logic, area-based resource formula, building/combat/targeting systems
- **Technical Architecture** — Full ECS data-flow with Mermaid diagrams, 25+ `IComponentData` definitions, 17+ `ISystem` catalogue, ScriptableObject→Entity mapping, event system, physics layers
- **UI/UX** — Mobile HUD layout, interaction model, health bars, selection feedback
- **Build & Deployment** — Android APK instructions

---

## Part B: Vertical Slice Code Blueprint ✅

### Entity Definitions (4 files)

| File | Component | Purpose |
|------|-----------|---------|
| [CastleTagAuthoring.cs](Vertical_Slice/EntityDefinitions/CastleTagAuthoring.cs) | `CastleTag` | Zero-size tag for the player's Castle |
| [EnemyTagAuthoring.cs](Vertical_Slice/EntityDefinitions/EnemyTagAuthoring.cs) | `EnemyTag` | Zero-size tag for enemy wave units |
| [ResourceGeneratorAuthoring.cs](Vertical_Slice/EntityDefinitions/ResourceGeneratorAuthoring.cs) | `ResourceGenerator` | Area-based Farm generation data |
| [PlayerResourcesAuthoring.cs](Vertical_Slice/EntityDefinitions/PlayerResourcesAuthoring.cs) | `PlayerResources` | Global resource pool singleton |

### Core Systems (3 files)

| File | System | Burst | Job |
|------|--------|-------|-----|
| [ResourceGenerationSystem.cs](Vertical_Slice/Systems/ResourceGenerationSystem.cs) | `ResourceGenerationSystem` | ✅ | — (`OverlapSphere` query) |
| [WaveMovementSystem.cs](Vertical_Slice/Systems/WaveMovementSystem.cs) | `WaveMovementSystem` | ✅ | `WaveMovementJob : IJobEntity` |
| [SimpleCombatSystem.cs](Vertical_Slice/Systems/SimpleCombatSystem.cs) | `SimpleCombatSystem` | ✅ | — (ECB pattern) |

### Build Instructions

[BUILD_INSTRUCTIONS.md](Vertical_Slice/BUILD_INSTRUCTIONS.md) — Step-by-step Android APK compilation with Burst + IL2CPP optimisation settings.

---

## Codebase Integrity

> [!CAUTION]
> **`DOTS_RTS_Prototype/` is READONLY.** No files were modified. All new code follows existing patterns.

- Same Baker convention (`Baker<T>`, `GetEntity`, `AddComponent`)
- Same `[BurstCompile]` + `ISystem` pattern
- Same `EntityUtil.ExistsAndPersists()` validation
- Same `OverlapSphere` physics query approach
- Same timer-based cooldown pattern (`generationPhaseTime`, `attackPhaseTime`)
- Same singleton pattern (`PlayerResources` mirrors `EntityPrefabsRegistry`, `UnitDataRegistry`)
