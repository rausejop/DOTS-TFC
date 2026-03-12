using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Burst-compiled system that provides simplified collision-based combat logic
/// for the Vertical Slice milestone. Handles damage application when melee
/// units physically overlap with their targets.
/// </summary>
/// <remarks>
/// This system complements the existing <c>MeleeAttackSystem</c> and
/// <c>ShootAttackSystem</c> by providing a lightweight, proximity-based
/// damage check for the Vertical Slice demonstration.
///
/// The system uses the existing <see cref="Targetter"/>, <see cref="Health"/>,
/// and <see cref="MeleeAttack"/> components. It does NOT replace the existing
/// attack systems — rather, it adds a fallback damage check for
/// <see cref="EnemyTag"/> entities that may lack full targeting components but
/// should still deal damage when colliding with the Castle.
///
/// Pattern: Follows the same entity query + distance check approach used in
/// <c>MeleeAttackSystem</c>, but specifically for wave enemies vs. Castle.
/// </remarks>
partial struct SimpleCombatSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CastleTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Find the Castle entity for damage application
        Entity castleEntity = Entity.Null;
        float3 castlePosition = float3.zero;

        foreach ((
            RefRO<LocalTransform> castleTransform,
            RefRO<CastleTag> castleTag,
            Entity entity)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRO<CastleTag>>().
                WithEntityAccess())
        {
            castleEntity = entity;
            castlePosition = castleTransform.ValueRO.Position;
            break; //Singleton
        }

        if (castleEntity == Entity.Null)
        {
            return;
        }

        //Check if castle still has health
        if (!SystemAPI.HasComponent<Health>(castleEntity))
        {
            return;
        }

        EntityCommandBuffer ecb =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        float deltaTime = SystemAPI.Time.DeltaTime;

        //Query all enemy-tagged units to check collision with the Castle
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRO<EnemyTag> enemyTag,
            RefRO<Unit> unit,
            Entity entity)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRO<EnemyTag>,
                RefRO<Unit>>().
                WithEntityAccess())
        {
            //Calculate distance to Castle
            float distanceToCastle = math.distance(localTransform.ValueRO.Position, castlePosition);

            //Get Castle's collider offset for proximity check
            float castleColliderOffset = 0f;
            if (SystemAPI.HasComponent<Building>(castleEntity))
            {
                castleColliderOffset = SystemAPI.GetComponent<Building>(castleEntity).colliderOffsetRadius;
            }

            //Collision check: unit radius + castle radius + a small contact threshold
            float contactThreshold = 1.5f;
            float minContactDistance = unit.ValueRO.colliderOffsetRadius + castleColliderOffset + contactThreshold;

            if (distanceToCastle < minContactDistance)
            {
                //Enemy is touching the Castle — apply damage
                //Use MeleeAttack damage if available, otherwise a default value
                int damageAmount = 1; //Default damage per frame of contact

                if (SystemAPI.HasComponent<MeleeAttack>(entity))
                {
                    RefRW<MeleeAttack> meleeAttack = SystemAPI.GetComponentRW<MeleeAttack>(entity);

                    //Respect cooldown timer
                    meleeAttack.ValueRW.attackPhaseTime -= deltaTime;
                    if (meleeAttack.ValueRO.attackPhaseTime > 0)
                    {
                        continue;
                    }

                    meleeAttack.ValueRW.attackPhaseTime = meleeAttack.ValueRO.attackFrequency;
                    damageAmount = meleeAttack.ValueRO.damageAmount;
                    meleeAttack.ValueRW.onAttack = true;
                }

                //Apply damage to Castle
                RefRW<Health> castleHealth = SystemAPI.GetComponentRW<Health>(castleEntity);
                castleHealth.ValueRW.currentHealth -= damageAmount;
                castleHealth.ValueRW.onHealthChanged = true;

                //Optionally: destroy the enemy on contact (siege unit behaviour)
                //Uncomment the line below for kamikaze-style enemies:
                // ecb.DestroyEntity(entity);
            }
        }
    }
}
