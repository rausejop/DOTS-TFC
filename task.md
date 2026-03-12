- [x] Analyze existing systems and design concurrent approaches.
- [x] Refactor `WaveMovementSystem` to use `SystemAPI.TryGetSingleton` and keep `ScheduleParallel`.
- [x] Refactor `ResourceGenerationSystem`
  - [x] Implement `IJobEntity` for overlap sphere calculation.
  - [x] Create `NativeQueue<float>` to pass generated resources safely.
  - [x] Implement `IJob` to sum queue items into `PlayerResources` singleton safely.
- [x] Refactor `SimpleCombatSystem`
  - [x] Implement `IJobEntity` for combat distance checks.
  - [x] Create `NativeQueue<int>` to accumulate damage safely from multiple enemies.
  - [x] Implement `IJob` to dequeue damage and apply it to Castle's `Health` component.
- [x] Verify there are no race conditions and compilation is successful.

# Task Breakdown: Architectural Fix
- [x] Remove `[NativeDisableParallelForRestriction]` and `ComponentLookup<MeleeAttack>` from `SimpleCombatSystem`.
- [x] Implement `CombatCheckWithMeleeJob : IJobEntity` with strict `[WithAll(typeof(MeleeAttack))]`.
- [x] Implement `CombatCheckWithoutMeleeJob : IJobEntity` with strict `[WithNone(typeof(MeleeAttack))]`.
- [x] Schedule both sequentially dependent jobs with the `ParallelWriter` queue.
