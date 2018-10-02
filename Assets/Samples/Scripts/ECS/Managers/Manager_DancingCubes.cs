using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Entities;
using System;

[RequireComponent(typeof(Camera))]
public sealed class Manager_DancingCubes : MonoBehaviour
{
    [SerializeField] MeshInstanceRenderer meshInstanceRenderer;
    [SerializeField] uint count;
    [SerializeField] uint2 range;
    [SerializeField] uint danceLoopLength;
    private void Start()
    {
        var manager = InitializeWorld();
        InitializeEntities(manager);
    }

    private void InitializeEntities(EntityManager manager)
    {
        if (count == 0) return;
        var entities = new NativeArray<Entity>((int)count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        try
        {
            entities[0] = manager.CreateEntity(ComponentType.Create<Position>(), ComponentType.Create<MeshInstanceRenderer>(), ComponentType.Create<StartTime>(), ComponentType.Create<Velocity>(), ComponentType.Create<DanceMove>(), ComponentType.Create<DanceSystem.Tag>());
            manager.SetSharedComponentData(entities[0], meshInstanceRenderer);
            manager.SetComponentData(entities[0], new StartTime { Value = Time.timeSinceLevelLoad });
            unsafe
            {
                var rest = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>(((Entity*)NativeArrayUnsafeUtility.GetUnsafePtr(entities)) + 1, entities.Length - 1, Allocator.Temp);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rest, NativeArrayUnsafeUtility.GetAtomicSafetyHandle(entities));
#endif
                manager.Instantiate(entities[0], rest);
            }
            var rand = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);
            for (int i = 0; i < entities.Length; i++)
                InitializeEntity(ref rand, manager, entities[i]);
        }
        finally
        {
            entities.Dispose();
        }
    }

    private void InitializeEntity(ref Unity.Mathematics.Random random, EntityManager manager, Entity entity)
    {
        var moves = manager.GetBuffer<DanceMove>(entity);
        DanceMove move = default;
        for (uint i = 0; i < danceLoopLength; ++i)
        {
            var values = random.NextFloat4() * 10f - 5f;
            move.Duration = values.w + 5f;
            move.Velocity = values.xyz;
            random.state = random.NextUInt();
            moves.Add(move);
        }
        manager.SetComponentData(entity, new Velocity { Value = (random.NextFloat3() - 0.5f) * 8f });
    }

    private EntityManager InitializeWorld()
    {
        var worlds = new World[1];
        ref var world = ref worlds[0];
        world = new World("Dancing");
        var manager = world.CreateManager<EntityManager>();
        world.CreateManager(typeof(MoveSystem));
        world.CreateManager(typeof(EndFrameBarrier));
        world.CreateManager(typeof(DanceSystem));
        world.CreateManager(typeof(EndFrameTransformSystem));
        world.CreateManager<MeshInstanceRendererSystem>().ActiveCamera = GetComponent<Camera>();
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(worlds);
        return manager;
    }
}