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
        //Retrieve the global resource pool singleton
        RefRW<PlayerResources> playerResources = SystemAPI.GetSingletonRW<PlayerResources>();

        //Register CollisionWorld for physics queries
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        //Reusable hit list (kept external to the loop to avoid excessive allocations)
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<ResourceGenerator> resourceGenerator)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<ResourceGenerator>>())
        {
            //Timer tick
            resourceGenerator.ValueRW.generationPhaseTime += deltaTime;
            if (resourceGenerator.ValueRO.generationPhaseTime < resourceGenerator.ValueRO.generationInterval)
            {
                continue;
            }

            //Reset timer
            resourceGenerator.ValueRW.generationPhaseTime = 0f;

            //Calculate total influence area (circle: π × r²)
            float radius = resourceGenerator.ValueRO.influenceRadius;
            float totalArea = math.PI * radius * radius;

            //Scan for nearby entities within the influence radius
            distanceHitList.Clear();
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u, //All layers
                CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                GroupIndex = 0
            };

            float occupiedArea = 0f;

            if (collisionWorld.OverlapSphere(
                    localTransform.ValueRO.Position,
                    radius,
                    ref distanceHitList,
                    collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    if (!EntityUtil.ExistsAndPersists(ref state, distanceHit.Entity))
                    {
                        continue;
                    }

                    //Accumulate occupied area from detected entities' collider radii
                    float entityRadius = 0f;

                    if (SystemAPI.HasComponent<Unit>(distanceHit.Entity))
                    {
                        entityRadius = SystemAPI.GetComponent<Unit>(distanceHit.Entity).colliderOffsetRadius;
                    }
                    else if (SystemAPI.HasComponent<Building>(distanceHit.Entity))
                    {
                        entityRadius = SystemAPI.GetComponent<Building>(distanceHit.Entity).colliderOffsetRadius;
                    }

                    //Approximate occupied area as a circle (π × r²)
                    occupiedArea += math.PI * entityRadius * entityRadius;
                }
            }

            //Calculate free area fraction (clamped to [0, 1])
            float freeAreaFraction = math.saturate((totalArea - occupiedArea) / totalArea);

            //Calculate generated resources for this tick
            float generatedResources = resourceGenerator.ValueRO.baseOutputRate * freeAreaFraction;

            //Deposit into the global resource pool
            playerResources.ValueRW.currentResources += generatedResources;
        }
    }
}
