using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Burst-compiled system that drives linear side-scrolling movement for enemy wave units.
/// All entities tagged with <see cref="EnemyTag"/> advance toward the player's Castle
/// (identified by <see cref="CastleTag"/>) along the X-Z gameplay plane.
/// </summary>
/// <remarks>
/// This is a simplified, deterministic movement system for wave enemies.
/// It queries for the Castle's position each frame and sets the
/// <see cref="UnitMover.targetPosition"/> accordingly, letting the existing
/// <c>UnitMoverSystem</c> (with its Burst-compiled <c>UnitMoverJob</c>) handle
/// the actual physics-based movement.
///
/// If no Castle exists (already destroyed), wave enemies stop moving.
/// </remarks>
partial struct WaveMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemyTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Find the Castle position (if it exists)
        float3 castlePosition = float3.zero;
        bool castleExists = false;

        foreach ((
            RefRO<LocalTransform> castleTransform,
            RefRO<CastleTag> castleTag)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRO<CastleTag>>())
        {
            castlePosition = castleTransform.ValueRO.Position;
            castleExists = true;
            break; //Singleton — only one Castle expected
        }

        if (!castleExists)
        {
            //No Castle — enemies have nothing to advance toward
            return;
        }

        //Schedule the wave movement job
        new WaveMovementJob
        {
            castlePosition = castlePosition,
        }.ScheduleParallel();
    }
}

/// <summary>
/// Burst-compiled parallel job that sets the <see cref="UnitMover.targetPosition"/>
/// for all <see cref="EnemyTag"/> entities to the Castle's position.
/// This creates a linear side-scrolling advance from the right side toward the Castle.
/// </summary>
/// <remarks>
/// The actual movement is handled by the existing <c>UnitMoverJob</c> in
/// <c>UnitMoverSystem</c>, which applies physics velocity and rotation interpolation.
/// This job only sets the destination.
///
/// Entities with <see cref="ManualMove"/> disabled and a valid <see cref="Targetter"/>
/// target will have their destination overridden by combat systems (chase behaviour),
/// which takes priority. This job handles the default "advance" behaviour for
/// enemies that have no combat target yet.
/// </remarks>
[BurstCompile]
public partial struct WaveMovementJob : IJobEntity
{
    //Set on struct construction
    public float3 castlePosition;

    public void Execute(
        ref UnitMover unitMover,
        in EnemyTag enemyTag,
        in Targetter targetter)
    {
        //Only set the destination if the enemy has no active combat target
        //This allows combat systems (MeleeAttack/ShootAttack) to override the
        //destination when engaging a specific target
        if (targetter.targetEntity == Entity.Null)
        {
            unitMover.targetPosition = castlePosition;
        }
    }
}
