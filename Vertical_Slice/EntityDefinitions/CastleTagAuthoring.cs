using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="CastleTag"/> unmanaged component.
/// Attach this to the player's Castle GameObject in the scene to mark it as the
/// primary defensive objective. The Castle is positioned at the far-left of the map;
/// the game ends if it is destroyed.
/// </summary>
class CastleTagAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="CastleTag"/> unmanaged component.
/// </summary>
class CastleTagBaker : Baker<CastleTagAuthoring>
{
    public override void Bake(CastleTagAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new CastleTag());
    }
}

/// <summary>
/// Zero-size tag component used to uniquely identify the player's Castle entity.
/// The Castle is the primary defensive objective — the game is lost if the
/// Castle's <see cref="Health"/> reaches zero.
/// </summary>
/// <remarks>
/// Expected to exist as a single entity in the world.
/// Systems may query for this tag to locate the Castle position for wave
/// pathfinding and win/loss condition checks.
/// </remarks>
public struct CastleTag : IComponentData
{
}
