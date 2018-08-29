using Unity.Jobs;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;

using UnityEngine;

[AlwaysUpdateSystem]
sealed class ClickSpawnCube : JobComponentSystem
{
    EntityArchetype entityArchetype;
    MeshInstanceRenderer cube;
    public ClickSpawnCube(MeshInstanceRenderer cube) => this.cube = cube;
    protected override void OnCreateManager(int capacity) => entityArchetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<MeshInstanceRenderer>(), ComponentType.Create<Position>(), ComponentType.Create<Velocity>());

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (Input.GetMouseButton(0))
        {
            var e = EntityManager.CreateEntity(entityArchetype);
            EntityManager.SetSharedComponentData(e, cube);
            EntityManager.SetComponentData(e, new Position { Value = new float3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 10f });
            EntityManager.SetComponentData(e, new Velocity(new float3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 10f));
        }
        return inputDeps;
    }
}