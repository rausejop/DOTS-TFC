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
        if (!SystemAPI.TryGetSingletonEntity<CastleTag>(out Entity castleEntity))
        {
            return;
        }

        if (!SystemAPI.HasComponent<Health>(castleEntity))
        {
            return;
        }

        float3 castlePosition = SystemAPI.GetComponent<LocalTransform>(castleEntity).Position;
        float castleColliderOffset = 0f;
        if (SystemAPI.HasComponent<Building>(castleEntity))
        {
            castleColliderOffset = SystemAPI.GetComponent<Building>(castleEntity).colliderOffsetRadius;
        }

        // Use TempJob allocator for the queue
        NativeQueue<int> damageQueue = new NativeQueue<int>(Allocator.TempJob);

        // 1. Schedule parallel job FOR ENEMIES WITH MELEE ATTACK
        CombatCheckWithMeleeJob combatWithMeleeJob = new CombatCheckWithMeleeJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            castlePosition = castlePosition,
            castleColliderOffset = castleColliderOffset,
            damageQueue = damageQueue.AsParallelWriter()
        };
        state.Dependency = combatWithMeleeJob.ScheduleParallel(state.Dependency);

        // 2. Schedule parallel job FOR ENEMIES WITHOUT MELEE ATTACK
        CombatCheckWithoutMeleeJob combatWithoutMeleeJob = new CombatCheckWithoutMeleeJob
        {
            castlePosition = castlePosition,
            castleColliderOffset = castleColliderOffset,
            damageQueue = damageQueue.AsParallelWriter()
        };
        state.Dependency = combatWithoutMeleeJob.ScheduleParallel(state.Dependency);

        // 3. Schedule single thread job to apply damage
        ApplyDamageJob applyJob = new ApplyDamageJob
        {
            castleEntity = castleEntity,
            healthLookup = SystemAPI.GetComponentLookup<Health>(false),
            damageQueue = damageQueue
        };
        state.Dependency = applyJob.Schedule(state.Dependency);

        // Dispose the queue after the apply damage job completes
        damageQueue.Dispose(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(MeleeAttack))]
public partial struct CombatCheckWithMeleeJob : IJobEntity
{
    public float deltaTime;
    public float3 castlePosition;
    public float castleColliderOffset;
    
    public NativeQueue<int>.ParallelWriter damageQueue;

    public void Execute(
        in LocalTransform localTransform,
        in EnemyTag enemyTag,
        in Unit unit,
        ref MeleeAttack meleeAttack)
    {
        //Calculate distance to Castle
        float distanceToCastle = math.distance(localTransform.Position, castlePosition);

        //Collision check: unit radius + castle radius + a small contact threshold
        float contactThreshold = 1.5f;
        float minContactDistance = unit.colliderOffsetRadius + castleColliderOffset + contactThreshold;

        if (distanceToCastle < minContactDistance)
        {
            //Respect cooldown timer
            meleeAttack.attackPhaseTime -= deltaTime;
            if (meleeAttack.attackPhaseTime > 0)
            {
                return;
            }

            meleeAttack.attackPhaseTime = meleeAttack.attackFrequency;
            int damageAmount = meleeAttack.damageAmount;
            meleeAttack.onAttack = true; // Flag for visual effects if any
            
            damageQueue.Enqueue(damageAmount);
        }
    }
}

[BurstCompile]
[WithAll(typeof(EnemyTag))]
[WithNone(typeof(MeleeAttack))]
public partial struct CombatCheckWithoutMeleeJob : IJobEntity
{
    public float3 castlePosition;
    public float castleColliderOffset;
    
    public NativeQueue<int>.ParallelWriter damageQueue;

    public void Execute(
        in LocalTransform localTransform,
        in Unit unit)
    {
        //Calculate distance to Castle
        float distanceToCastle = math.distance(localTransform.Position, castlePosition);

        //Collision check: unit radius + castle radius + a small contact threshold
        float contactThreshold = 1.5f;
        float minContactDistance = unit.colliderOffsetRadius + castleColliderOffset + contactThreshold;

        if (distanceToCastle < minContactDistance)
        {
            //Enemy is touching the Castle — apply default damage
            int damageAmount = 1; //Default damage per frame of contact
            damageQueue.Enqueue(damageAmount);
        }
    }
}

[BurstCompile]
public struct ApplyDamageJob : IJob
{
    public NativeQueue<int> damageQueue;
    public Entity castleEntity;
    public ComponentLookup<Health> healthLookup;

    public void Execute()
    {
        int totalDamage = 0;
        
        while (damageQueue.TryDequeue(out int result))
        {
            totalDamage += result;
        }

        if (totalDamage > 0 && healthLookup.HasComponent(castleEntity))
        {
            Health castleHealth = healthLookup[castleEntity];
            castleHealth.currentHealth -= totalDamage;
            castleHealth.onHealthChanged = true;
            healthLookup[castleEntity] = castleHealth;
        }
    }
}
