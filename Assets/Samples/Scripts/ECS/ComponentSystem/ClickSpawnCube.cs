using Unity.Jobs;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;

using UnityEngine;

[AlwaysUpdateSystem]
[UpdateBefore(typeof(MoveSystem))]
sealed class ClickSpawnCube : ComponentSystem
{
    EntityArchetype entityArchetype;
    MeshInstanceRenderer cube;
    public ClickSpawnCube(MeshInstanceRenderer cube) => this.cube = cube;
    protected override void OnCreateManager() => entityArchetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<MeshInstanceRenderer>(), ComponentType.Create<Position>(), ComponentType.Create<Velocity>());

    protected override void OnUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            var e = EntityManager.CreateEntity(entityArchetype);
            EntityManager.SetSharedComponentData(e, cube);
            Set(e);
        }
    }

    private void Set(Entity e)
    {
        EntityManager.SetComponentData(e, new Position { Value = new float3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 10f });
        EntityManager.SetComponentData(e, new Velocity(new float3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 10f));
    }
}