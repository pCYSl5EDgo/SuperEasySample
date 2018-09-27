using UnityEngine;
using UnityEngine.Rendering;

using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;

[RequireComponent(typeof(Camera))]
sealed class Manager_MovingCubes : MonoBehaviour
{
    [SerializeField] MeshInstanceRenderer[] renderers;

    void Start()
    {
        World.Active = new World("move cube");
        manager = World.Active.CreateManager<EntityManager>();
        World.Active.CreateManager(typeof(EndFrameTransformSystem));
        World.Active.CreateManager<MeshInstanceRendererSystem>().ActiveCamera = GetComponent<Camera>();
        World.Active.CreateManager(typeof(MoveSystem));
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);

        archetype = manager.CreateArchetype(ComponentType.Create<Position>(), ComponentType.Create<Velocity>(), ComponentType.Create<MeshInstanceRenderer>());

        var src = manager.CreateEntity(archetype);
        renderers[0].material.enableInstancing = true;
        manager.SetSharedComponentData(src, renderers[0]);
        Set(src);
        using (var _ = new NativeArray<Entity>(11450, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
        {
            manager.Instantiate(src, _);
            for (int i = 0; i < _.Length; i++)
                Set(_[i]);
        }
    }

    EntityManager manager;
    EntityArchetype archetype;

    private void Set(in Entity e)
    {
        manager.SetComponentData(e, new Position { Value = new Unity.Mathematics.float3((Random.value - 0.5f) * 40, (Random.value - 0.5f) * 40, (Random.value - 0.5f) * 40) });
        manager.SetComponentData(e, new Velocity { Value = new Unity.Mathematics.float3((Random.value - 0.5f) * 40, (Random.value - 0.5f) * 40, (Random.value - 0.5f) * 40) });
    }
}