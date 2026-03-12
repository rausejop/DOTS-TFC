# DOTS RTS Prototype — Android Build Instructions

> **Target:** Android APK via Unity's build pipeline  
> **Engine:** Unity 2022.3+ with DOTS packages  
> **Language Standard:** Oxford English  
> **Generated:** 2026-03-13  
> **`DOTS_RTS_Prototype/`:** READONLY — context only, no modifications  

### Related Documents

| Document | Description |
|----------|-------------|
| [README.md](../README.md) | Project overview, badges, and file manifest |
| [Game Design Document](../Game_Design_Document.md) | Full GDD with technical architecture |
| [Implementation Plan](../implementation_plan.md) | Deliverables plan and design decisions |
| [Walkthrough](../walkthrough.md) | Summary of all generated deliverables |

---

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| Unity Editor | 2022.3 LTS+ | Must include Android Build Support module. |
| Android SDK | API 30+ | Installed via Unity Hub → Add Modules. |
| Android NDK | r23b+ | Required for IL2CPP compilation. |
| JDK | 11 | Bundled with Unity Hub's Android module. |

### Required Unity Packages (via Package Manager)

| Package | Minimum Version |
|---------|-----------------|
| `com.unity.entities` | 1.0.0 |
| `com.unity.entities.graphics` | 1.0.0 |
| `com.unity.physics` | 1.0.0 |
| `com.unity.burst` | 1.8.0 |
| `com.unity.collections` | 2.1.0 |
| `com.unity.rendering.hybrid` | 0.51+ (if not using entities.graphics) |

---

## Step 1: Integration of Vertical Slice Code

Copy the generated Vertical Slice files into the existing project structure:

```
Vertical_Slice/EntityDefinitions/CastleTagAuthoring.cs
  → Assets/Scripts/Authoring/Buildings/CastleTagAuthoring.cs

Vertical_Slice/EntityDefinitions/EnemyTagAuthoring.cs
  → Assets/Scripts/Authoring/Units/EnemyTagAuthoring.cs

Vertical_Slice/EntityDefinitions/ResourceGeneratorAuthoring.cs
  → Assets/Scripts/Authoring/Buildings/ResourceGeneratorAuthoring.cs

Vertical_Slice/EntityDefinitions/PlayerResourcesAuthoring.cs
  → Assets/Scripts/Authoring/Common/PlayerResourcesAuthoring.cs

Vertical_Slice/Systems/ResourceGenerationSystem.cs
  → Assets/Scripts/Systems/Buildings/ResourceGenerationSystem.cs

Vertical_Slice/Systems/WaveMovementSystem.cs
  → Assets/Scripts/Systems/Units/WaveMovementSystem.cs

Vertical_Slice/Systems/SimpleCombatSystem.cs
  → Assets/Scripts/Systems/Attacks/SimpleCombatSystem.cs
```

### Scene Setup for New Components

1. **Castle:** Add `CastleTagAuthoring` to the Castle GameObject.
2. **Enemy prefabs:** Add `EnemyTagAuthoring` to each enemy wave unit prefab.
3. **Farm buildings:** Add `ResourceGeneratorAuthoring` to Farm/Production building prefabs.
4. **Scene singleton:** Create an empty GameObject named `PlayerResourcesManager` and add `PlayerResourcesAuthoring` — set the starting resources value.

---

## Step 2: Player Settings

Navigate to **Edit → Project Settings → Player**.

### Other Settings

| Setting | Value | Rationale |
|---------|-------|-----------|
| **Scripting Backend** | IL2CPP | Required for Burst AOT compilation on Android. Mono is not supported for DOTS production builds. |
| **Target Architectures** | ARM64 | All modern Android devices use ARM64. Disabling ARMv7 reduces APK size. |
| **API Compatibility Level** | .NET Standard 2.1 | Required by Unity Entities. |
| **Managed Stripping Level** | High | Removes unused managed code, reducing APK size. |
| **C++ Compiler Configuration** | Release | Enables full optimisation for IL2CPP-generated code. |
| **Minimum API Level** | Android 7.0 (API 24) | Minimum for Vulkan support and modern DOTS. |

### Resolution and Presentation

| Setting | Value |
|---------|-------|
| **Default Orientation** | Landscape Left |
| **Allowed Orientations** | Landscape Left + Landscape Right |
| **Render Outside Safe Area** | ✅ |
| **Optimised Frame Pacing** | ✅ |

### Graphics

| Setting | Value |
|---------|-------|
| **Graphics APIs** | Vulkan (primary), OpenGLES 3.2 (fallback) |
| **Colour Space** | Linear |

---

## Step 3: Burst Compiler Settings

Navigate to **Jobs → Burst → Burst AOT Settings** or check the Player Settings.

| Setting | Value | Rationale |
|---------|-------|-----------|
| **Enable Burst Compilation** | ✅ | All `[BurstCompile]` systems and jobs require this. |
| **Safety Checks** | Disabled | Release builds — safety checks add significant overhead. Enable only for debugging. |
| **Optimisation Level** | Standard (LLVM -O2) | Best balance of compile time and runtime performance. |
| **Target Platform** | Android | Ensure ARM NEON SIMD is utilised. |
| **Enable Debug Information** | Disabled | Release only — reduces binary size. |

> **Debugging Tip:** During development, set Safety Checks to **Force On** and Optimisation Level to **Level 0** to catch `NativeArray` out-of-bounds errors and other safety violations.

---

## Step 4: Quality & Rendering Settings

Navigate to **Edit → Project Settings → Quality**.

### Recommended Mobile Profile

| Setting | Value |
|---------|-------|
| **VSyncCount** | Don't Sync |
| **Target Frame Rate** | 60 (set via `Application.targetFrameRate = 60;`) |
| **Shadow Resolution** | Low (512) |
| **Shadow Distance** | 30 |
| **Anti-Aliasing** | 2x MSAA |
| **Texture Quality** | Full |
| **Anisotropic Textures** | Per Texture |

### URP/Pipeline Settings (if using URP)

- Disable **HDR** for mobile.
- Set **MSAA** to 2x.
- Disable **Depth Texture** unless required.
- Set **Render Scale** to 0.85–1.0 depending on target device.

---

## Step 5: Build the APK

1. Open **File → Build Settings**.
2. Select **Android** as the platform.
3. Click **Switch Platform** (if not already selected).
4. Ensure the correct scenes are included in the build list.
5. Configure:

| Setting | Value |
|---------|-------|
| **Build System** | Gradle |
| **Export Project** | ❌ (build APK directly) |
| **Texture Compression** | ASTC |
| **Compression Method** | LZ4HC |
| **Split Application Binary** | ❌ |

6. Click **Build** or **Build and Run** (with device connected via USB).

### Build Command (CLI alternative)

```bash
Unity.exe -batchmode -nographics \
  -projectPath "path/to/DOTS_RTS_Prototype" \
  -buildTarget Android \
  -executeMethod BuildScript.PerformBuild \
  -quit -logFile build.log
```

---

## Step 6: Performance Validation

After deploying the APK to the device:

1. Ensure `MobilePerformanceMonitor` is attached to a scene GameObject.
2. Verify the following on the target device:

| Metric | Target | Measured With |
|--------|--------|---------------|
| **FPS** | ≥ 60 sustained | `MobilePerformanceMonitor.fps` |
| **Avg FPS (10s)** | ≥ 55 | `MobilePerformanceMonitor.avgFps` |
| **GPU Frame Time** | < 16.6 ms | `FrameTimingManager` |
| **Job Threads** | = CPU Core Count − 1 | `JobsUtility.JobWorkerCount` |
| **Entity Count** | Stable under load | Unity Profiler → Entities module |

3. Use the **Unity Profiler** (connected via USB or Wi-Fi) to verify:
   - Burst-compiled systems show `[Burst]` label in the Profiler timeline.
   - No GC allocations in hot paths (systems should be zero-alloc).
   - Job system utilisation across all available worker threads.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `Burst compilation failed` | Ensure IL2CPP backend is selected; check Burst package version compatibility. |
| `PhysicsWorldSingleton not found` | Ensure `com.unity.physics` is installed and a physics scene is properly configured. |
| `EntityCommandBuffer errors` | Verify `EndSimulationEntityCommandBufferSystem` is present (auto-created by Entities package). |
| `Low FPS on device` | Reduce Shadow Resolution; disable unnecessary post-processing; check entity count with Profiler. |
| `APK too large` | Enable Managed Stripping Level: High; use ASTC textures; strip unused engine modules in Player Settings. |
| `Gradle build failure` | Update Gradle to the version bundled with Unity; ensure Android SDK/NDK paths are correctly set. |

---

*Build instructions generated for DOTS RTS Prototype Vertical Slice.*
