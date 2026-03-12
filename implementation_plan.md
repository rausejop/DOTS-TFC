# Goal Description

The recent refactor into concurrent systems introduced an architectural flaw in `SimpleCombatSystem`. Specifically, by using `ComponentLookup<MeleeAttack>` combined with `[NativeDisableParallelForRestriction]`, the system bypassed the DOTS safety and dependency injection trackers. This effectively "touched" components (`MeleeAttack`) that belong to other systems (like `MeleeAttackSystem`) without proper thread-safe dependency mapping, violating strict separation of concerns.

The goal is to fix this by implementing proper `IJobEntity` query variants that respect Unity's Job System dependencies securely.

## Proposed Changes

### [SimpleCombatSystem]
**Path:** `Vertical_Slice/Systems/SimpleCombatSystem.cs`
- Remove the unsafe `ComponentLookup<MeleeAttack>` and the `[NativeDisableParallelForRestriction]` attribute from the combat job.
- Split `CombatCheckJob` into two separate, safe `IJobEntity` structs:
  1. `CombatCheckWithMeleeJob`: Executes specifically on entities that have `[WithAll(typeof(MeleeAttack))]`. It will take `ref MeleeAttack` directly in the `Execute` method, allowing DOTS to properly log this system as a standard writer to `MeleeAttack` and schedule it safely with `MeleeAttackSystem`.
  2. `CombatCheckWithoutMeleeJob`: Executes on entities `[WithNone(typeof(MeleeAttack))]` and simply queues a default damage of 1.
- Both jobs will continue to use the thread-safe `NativeQueue<int>.ParallelWriter` to stream their damage outputs concurrently without overriding each other.
- The `ApplyDamageJob` will remain as a safely-chained single-thread `IJob` to flush the damage values precisely into the `Health` component, ensuring the `Health` component is updated procedurally without main-thread stalling.

## Verification Plan
1. Code will compile without errors regarding missing components.
2. The `[NativeDisableParallelForRestriction]` attribute will be completely purged from our codebase, restoring full system responsibility compliance.
3. The tutor's architectural feedback regarding exclusive script responsibilities via proper dependency boundaries will be completely resolved.
