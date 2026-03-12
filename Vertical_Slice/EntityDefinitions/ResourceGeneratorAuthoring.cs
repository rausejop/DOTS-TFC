using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="ResourceGenerator"/> unmanaged component.
/// Attach this to Farm building GameObjects to enable area-based resource generation.
/// </summary>
/// <remarks>
/// The resource output is calculated based on the free area within the Farm's influence
/// radius. Crowding the Farm with other buildings or units reduces its output.
/// </remarks>
class ResourceGeneratorAuthoring : MonoBehaviour
{
    /// <summary>
    /// Base resource output per generation tick when the entire influence area is free.
    /// </summary>
    [Tooltip("Base resource output per generation tick when the entire influence area is free.")]
    public float baseOutputRate = 10f;

    /// <summary>
    /// Radius around the Farm used to calculate free vs. occupied area.
    /// </summary>
    [Tooltip("Radius around the Farm used to calculate free vs. occupied area.")]
    public float influenceRadius = 15f;

    /// <summary>
    /// Time interval (in seconds) between resource generation ticks.
    /// </summary>
    [Tooltip("Time interval (in seconds) between resource generation ticks.")]
    public float generationInterval = 2f;
}

/// <summary>
/// Baker for the <see cref="ResourceGenerator"/> unmanaged component.
/// </summary>
class ResourceGeneratorBaker : Baker<ResourceGeneratorAuthoring>
{
    public override void Bake(ResourceGeneratorAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ResourceGenerator
        {
            baseOutputRate = authoring.baseOutputRate,
            influenceRadius = authoring.influenceRadius,
            generationInterval = authoring.generationInterval,
            generationPhaseTime = 0f,
            accumulatedResources = 0f
        });
    }
}

/// <summary>
/// Used by Farm buildings to generate "Resources" (the single in-game currency).
/// Output is calculated via an area-based formula:
/// <c>R = baseOutputRate × (freeArea / totalArea)</c>
/// where <c>totalArea = π × influenceRadius²</c> and <c>freeArea</c> is determined by
/// subtracting occupied collider areas detected via an <c>OverlapSphere</c> physics query.
/// </summary>
/// <remarks>
/// Requires the <see cref="Building"/> component on the same entity.
/// Processed by <c>ResourceGenerationSystem</c>.
/// </remarks>
public struct ResourceGenerator : IComponentData
{
    /// <summary>
    /// Base resource output per generation tick when the entire influence area is free.
    /// </summary>
    public float baseOutputRate;

    /// <summary>
    /// Radius around the Farm used to calculate free vs. occupied area.
    /// </summary>
    public float influenceRadius;

    /// <summary>
    /// Resources accumulated since the last deposit into the global <see cref="PlayerResources"/> pool.
    /// </summary>
    public float accumulatedResources;

    /// <summary>
    /// Current time elapsed since the last generation tick.
    /// </summary>
    public float generationPhaseTime;

    /// <summary>
    /// Time interval (in seconds) between resource generation ticks.
    /// </summary>
    public float generationInterval;
}
