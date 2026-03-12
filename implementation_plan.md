# DOTS RTS Prototype — Deliverables Implementation Plan

> **Generated:** 2026-03-13  
> **`DOTS_RTS_Prototype/`:** READONLY — context only, no modifications  

### Related Documents

| Document | Description |
|----------|-------------|
| [README.md](README.md) | Project overview, badges, and file manifest |
| [Game Design Document](Game_Design_Document.md) | Part A deliverable |
| [Walkthrough](walkthrough.md) | Summary of all generated deliverables |
| [Task Checklist](task.md) | Deliverables tracking |

## Completed ✅

### Part A: Game Design Document
- **File:** [Game_Design_Document.md](file:///c:/_CONFIANZA23/PRODUCTOS/29_DOT_TFG/DOTS-TFC-main/Game_Design_Document.md)
- Comprehensive GDD covering Executive Summary, Gameplay Mechanics, Technical Architecture (full ECS data-flow, all IComponentData definitions, ISystem catalogue), UI/UX, and Build Instructions.
- Derived entirely from analysis of the existing C# source files in `DOTS_RTS_Prototype/Assets/Scripts/` (READONLY).

---

## Completed: Part B — Compilable Code Blueprint ✅

> [!IMPORTANT]
> Per the prompt's security rule: **No files inside `DOTS_RTS_Prototype/` will be modified.** All new files are generated in the project root folder (`DOTS-TFC-main/`).

### Overview

Part B delivers the **Vertical Slice milestone** code — the three NEW systems and components that the existing codebase is missing per the Prompt.txt specification:

1. **`CastleTag`** + **`EnemyTag`** — Zero-size tag components to identify the Castle and enemy-wave units.
2. **`ResourceGeneratorComponent`** — Area-based resource generation for Farms.
3. **`ResourceGenerationSystem`** — Burst-compiled system implementing the area-based resource calculation.

Plus a consolidated build-instructions file.

### File Manifest

| # | File to Create | Category | Description |
|---|---------------|----------|-------------|
| 1 | `Vertical_Slice/EntityDefinitions/CastleTagAuthoring.cs` | Entity Definition | `CastleTag : IComponentData` tag + Authoring + Baker |
| 2 | `Vertical_Slice/EntityDefinitions/EnemyTagAuthoring.cs` | Entity Definition | `EnemyTag : IComponentData` tag + Authoring + Baker |
| 3 | `Vertical_Slice/EntityDefinitions/ResourceGeneratorAuthoring.cs` | Entity Definition | `ResourceGenerator : IComponentData` + Authoring + Baker |
| 4 | `Vertical_Slice/EntityDefinitions/PlayerResourcesAuthoring.cs` | Entity Definition | `PlayerResources : IComponentData` singleton for global resource count |
| 5 | `Vertical_Slice/Systems/ResourceGenerationSystem.cs` | Core System | Burst-compiled `ISystem` with area-based farm calculation |
| 6 | `Vertical_Slice/Systems/WaveMovementSystem.cs` | Core System | Burst-compiled `IJobEntity` — linear side-scrolling for enemy waves |
| 7 | `Vertical_Slice/Systems/SimpleCombatSystem.cs` | Core System | Burst-compiled collision/damage logic for the vertical slice |
| 8 | `Vertical_Slice/BUILD_INSTRUCTIONS.md` | Documentation | Step-by-step Android APK build guide |

### Design Decisions

> [!NOTE]
> **Why new files instead of modifying existing ones?**
> The prompt explicitly states: *"Don't modify files inside the DOTS_RTS_Prototype directory; only generate new files in current folder, based in context of DOTS_RTS_Prototype directory."*
> 
> The new code files are designed to be **drop-in compatible** with the existing codebase. They follow the exact same patterns (same coding style, same namespaces, same Baker pattern, same `EntityUtil`/`RegistryAccessor` utilities) so they can be copied into the `Assets/Scripts/` directories and work immediately.

### Architecture Alignment

The new code follows the exact patterns observed in the existing codebase:

| Pattern | Existing Example | New File |
|---------|-----------------|----------|
| Tag component (zero-size) | N/A (none exist yet) | `CastleTag`, `EnemyTag` |
| `IComponentData` + Baker | `HealthAuthoring.cs`, `BuildingAuthoring.cs` | `ResourceGeneratorAuthoring.cs` |
| Singleton component | `EntityPrefabsRegistry`, `UnitDataRegistry` | `PlayerResources` |
| `ISystem` with `[BurstCompile]` | `UnitMoverSystem`, `SpawnerSystem` | `ResourceGenerationSystem`, `WaveMovementSystem` |
| `IJobEntity` + `[BurstCompile]` | `UnitMoverJob` | `WaveMovementJob` |
| Physics `OverlapSphere` | `SpawnerSystem`, `FindTargetSystem` | `ResourceGenerationSystem` |
| `EntityCommandBuffer` pattern | `HealthSystem`, `TrainerSystem` | `SimpleCombatSystem` |
| Timer-based cooldown | `MeleeAttack.attackPhaseTime`, `Spawner.spawnPhaseTime` | `ResourceGenerator.generationPhaseTime` |

### Task Breakdown

#### Task 1: Entity Definitions (4 files)
Create the new `IComponentData` structs, their `MonoBehaviour` Authorings, and their Bakers — following the exact same pattern as existing files like `HealthAuthoring.cs`.

#### Task 2: Core Systems (3 files)
Create the three Burst-compiled systems required by the Prompt.txt:
- **ResourceGenerationSystem** — Uses `OverlapSphere` to calculate free area, computes resource output.
- **WaveMovementSystem** — Simple linear movement for enemy wave units (side-scrolling toward the Castle).
- **SimpleCombatSystem** — Basic collision detection and damage application.

#### Task 3: Build Instructions (1 file)
Detailed Android APK build guide with Burst + IL2CPP settings.

---

> [!TIP]
> All 8 files have been generated successfully. Each file is fully documented with XML summary comments (matching the existing codebase style) and ready to be integrated into the Unity project. See [walkthrough.md](walkthrough.md) for verification.
