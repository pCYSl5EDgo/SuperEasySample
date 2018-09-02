using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;

[RequireComponent(typeof(Camera))]
public sealed class Manager_MovingSpheres : MonoBehaviour
{
    [SerializeField] ComputeShader shader;
    [SerializeField] MeshInstanceRenderer[] renderers;
    Camera mainCamera;
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        var world = World.Active = new World("DrawMeshInstancedIncirect");
        world.SetDefaultCapacity(114514);
        manager = world.CreateManager<EntityManager>();
        world.CreateManager(typeof(MeshInstanceRendererInstancedIndirectSystem), mainCamera, renderers, shader);
        world.CreateManager(typeof(MoveSystem));
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);

        archetype = manager.CreateArchetype(ComponentType.Create<Position>(), ComponentType.Create<Velocity>(), ComponentType.Create<MeshInstanceRendererInstancedIndirectSystem.MeshInstanceRendererIndex>());
        var src = manager.CreateEntity(archetype);
        manager.SetSharedComponentData(src, new MeshInstanceRendererInstancedIndirectSystem.MeshInstanceRendererIndex(1u));
        Debug.Log(renderers[0].castShadows);
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
        manager.SetComponentData(e, new Velocity { Value = new Unity.Mathematics.float3((Random.value - 0.5f), (Random.value - 0.5f), (Random.value - 0.5f)) });
    }
}