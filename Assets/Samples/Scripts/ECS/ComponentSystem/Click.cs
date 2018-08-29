using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

[AlwaysUpdateSystem]
sealed class ClickSystem : ComponentSystem
{
    EntityArchetype entityArchetype;
    ComponentGroup g;
    protected override void OnCreateManager(int capacity)
    {
        var componentTypes = new ComponentType[] { ComponentType.ReadOnly<Count>() };
        entityArchetype = EntityManager.CreateArchetype(componentTypes);
        g = GetComponentGroup(componentTypes);
    }

    protected override void OnUpdate()
    {
        if (Input.GetMouseButton(0))
            EntityManager.CreateEntity(entityArchetype);
        else if (Input.GetMouseButton(1))
        {
            var source = g.GetEntityArray();
            if (source.Length == 0)
                return;
            using (var results = new NativeArray<Entity>(source.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            {
                new CopyEntities
                {
                    Results = results,
                    Source = source,
                }.Schedule(source.Length, 256).Complete();
                EntityManager.DestroyEntity(results);
            }
        }
    }
}