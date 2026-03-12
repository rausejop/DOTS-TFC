# Walkthrough

## Refactoring for Concurrency
The goal was successfully implemented by modernizing three major systems (`WaveMovementSystem`, `ResourceGenerationSystem`, and `SimpleCombatSystem`) to utilize Unity's DOTS Job System concurrently. Instead of looping across all matching entities within the main thread (blocking operations), computations are scattered using `.ScheduleParallel()`.

### Modifications
1. **[WaveMovementSystem](file:///c:/_CONFIANZA23/PRODUCTOS/29_DOT_TFG/DOTS-TFC-main/Vertical_Slice/Systems/WaveMovementSystem.cs)**
   - Deprecated the `foreach` iteration in favor of `SystemAPI.TryGetSingletonEntity<CastleTag>()`. This guarantees quick, O(1) discovery of the global Castle. The pre-existing parallel job continues routing units toward coordinates.
2. **[ResourceGenerationSystem](file:///c:/_CONFIANZA23/PRODUCTOS/29_DOT_TFG/DOTS-TFC-main/Vertical_Slice/Systems/ResourceGenerationSystem.cs)**
   - Replaced linear `PhysicsWorld` evaluations using a new Burst-compiled job, `GenerateResourcesJob : IJobEntity`. 
   - Introduced a `NativeQueue<float>` allocated with `Allocator.TempJob`. Since iterating the `collisionWorld` for hundreds of Farms can be incredibly expensive, offloading logic here avoids main thread stalling.
   - Outputs are safely injected concurrently using `NativeQueue<float>.ParallelWriter`, which is then digested iteratively into the `PlayerResource` singleton by `SumResourcesJob`.
3. **[SimpleCombatSystem](file:///c:/_CONFIANZA23/PRODUCTOS/29_DOT_TFG/DOTS-TFC-main/Vertical_Slice/Systems/SimpleCombatSystem.cs)**
   - Added asynchronous checks via `CombatCheckJob : IJobEntity`. Checks min-distance overlaps to the Castle dynamically.
   - Since multiple enemy logic processes may hit cooldown checks simultaneously, we inject damage outputs directly into a thread-safe `NativeQueue<int>.ParallelWriter`.
   - Extraneous side effects or entity updates are mitigated safely. `ApplyDamageJob` unpacks the damage pipeline queue down the road.

## Verification Required
As Unity projects utilizing Burst and DOTS depend on the Editor or msbuild generation logic internally for generating assemblies natively, checking syntax involves allowing the compiler environment:
- Open the Unity Editor for the project.
- Upon reload, check the console for any C# Compilation errors. None are anticipated.
- Play the **Vertical Slice** or Game scene to simulate resource collection rates and wave collision attacks. Verify logic behaves concurrently as intended.
