using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="EnemyTag"/> unmanaged component.
/// Attach this to enemy-wave unit prefabs to mark them as hostile entities
/// that advance linearly from the right side of the map toward the Castle.
/// </summary>
class EnemyTagAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="EnemyTag"/> unmanaged component.
/// </summary>
class EnemyTagBaker : Baker<EnemyTagAuthoring>
{
    public override void Bake(EnemyTagAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyTag());
    }
}

/// <summary>
/// Zero-size tag component used to identify enemy-wave units.
/// Entities with this tag are spawned by <see cref="Spawner"/> points at the
/// far-right of the map and advance linearly toward the player's Castle.
/// </summary>
/// <remarks>
/// Used by <c>WaveMovementSystem</c> to apply side-scrolling movement and by
/// combat systems to differentiate wave enemies from other hostile entities.
/// </remarks>
public struct EnemyTag : IComponentData
{
}
