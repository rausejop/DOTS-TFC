using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Burst-compiled system that calculates area-based resource generation for Farm buildings.
/// Each Farm's output is proportional to the free area within its influence radius:
/// <c>R = baseOutputRate × (freeArea / totalArea)</c>.
/// Generated resources are deposited into the global <see cref="PlayerResources"/> singleton.
/// </summary>
/// <remarks>
/// Uses an <c>OverlapSphere</c> physics query to detect nearby entities within each Farm's
/// influence radius. The occupied area is estimated from each entity's
/// <see cref="Unit.colliderOffsetRadius"/> or <see cref="Building.colliderOffsetRadius"/>.
/// This mirrors the physics query pattern used in <c>SpawnerSystem</c> and <c>FindTargetSystem</c>.
/// </remarks>
partial struct ResourceGenerationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerResources>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        // Use TempJob allocator for the queue since it will be passed to a job
        NativeQueue<float> generatedResourcesQueue = new NativeQueue<float>(Allocator.TempJob);

        // Schedule parallel job to calculate resources
        GenerateResourcesJob generateResourcesJob = new GenerateResourcesJob
        {
            deltaTime = deltaTime,
            collisionWorld = collisionWorld,
            unitsLayer = 1u << GameAssets.UNITS_LAYER,
            buildingsLayer = 1u << GameAssets.BUILDINGS_LAYER,
            unitLookup = SystemAPI.GetComponentLookup<Unit>(true),
            buildingLookup = SystemAPI.GetComponentLookup<Building>(true),
            resourceQueue = generatedResourcesQueue.AsParallelWriter()
        };
        
        state.Dependency = generateResourcesJob.ScheduleParallel(state.Dependency);

        // Schedule single thread job to sum resources and apply to singleton
        SumResourcesJob sumResourcesJob = new SumResourcesJob
        {
            resourceQueue = generatedResourcesQueue,
            playerResourcesEntity = SystemAPI.GetSingletonEntity<PlayerResources>(),
            playerResourcesLookup = SystemAPI.GetComponentLookup<PlayerResources>(false)
        };
        
        state.Dependency = sumResourcesJob.Schedule(state.Dependency);

        // Dispose the queue after the sum job completes
        generatedResourcesQueue.Dispose(state.Dependency);
    }
}

[BurstCompile]
public partial struct GenerateResourcesJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public CollisionWorld collisionWorld;
    public uint unitsLayer;
    public uint buildingsLayer;
    [ReadOnly] public ComponentLookup<Unit> unitLookup;
    [ReadOnly] public ComponentLookup<Building> buildingLookup;
    public NativeQueue<float>.ParallelWriter resourceQueue;

    public void Execute(
        Entity entity,
        in LocalTransform localTransform,
        ref ResourceGenerator resourceGenerator)
    {
        //Timer tick
        resourceGenerator.generationPhaseTime += deltaTime;
        if (resourceGenerator.generationPhaseTime < resourceGenerator.generationInterval)
        {
            return;
        }

        //Reset timer
        resourceGenerator.generationPhaseTime = 0f;

        //Calculate total influence area (circle: π × r²)
        float radius = resourceGenerator.influenceRadius;
        float totalArea = math.PI * radius * radius;

        CollisionFilter collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u, //All layers
            CollidesWith = unitsLayer | buildingsLayer,
            GroupIndex = 0
        };

        float occupiedArea = 0f;
        
        // NativeList allocated with Temp since it's local to the job execution
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        if (collisionWorld.OverlapSphere(
                localTransform.Position,
                radius,
                ref distanceHitList,
                collisionFilter))
        {
            foreach (DistanceHit distanceHit in distanceHitList)
            {
                // Accumulate occupied area from detected entities' collider radii
                float entityRadius = 0f;

                if (unitLookup.HasComponent(distanceHit.Entity))
                {
                    entityRadius = unitLookup[distanceHit.Entity].colliderOffsetRadius;
                }
                else if (buildingLookup.HasComponent(distanceHit.Entity))
                {
                    entityRadius = buildingLookup[distanceHit.Entity].colliderOffsetRadius;
                }
                else
                {
                    continue;
                }

                //Approximate occupied area as a circle (π × r²)
                occupiedArea += math.PI * entityRadius * entityRadius;
            }
        }
        
        distanceHitList.Dispose();

        //Calculate free area fraction (clamped to [0, 1])
        float freeAreaFraction = math.saturate((totalArea - occupiedArea) / totalArea);

        //Calculate generated resources for this tick
        float generatedResources = resourceGenerator.baseOutputRate * freeAreaFraction;

        if (generatedResources > 0f)
        {
            resourceQueue.Enqueue(generatedResources);
        }
    }
}

[BurstCompile]
public struct SumResourcesJob : IJob
{
    public NativeQueue<float> resourceQueue;
    public Entity playerResourcesEntity;
    public ComponentLookup<PlayerResources> playerResourcesLookup;

    public void Execute()
    {
        float totalGenerated = 0f;
        while (resourceQueue.TryDequeue(out float result))
        {
            totalGenerated += result;
        }

        if (totalGenerated > 0f)
        {
            PlayerResources resources = playerResourcesLookup[playerResourcesEntity];
            resources.currentResources += totalGenerated;
            playerResourcesLookup[playerResourcesEntity] = resources;
        }
    }
}
