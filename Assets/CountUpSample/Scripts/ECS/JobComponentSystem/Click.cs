using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;

using UnityEngine;

[AlwaysUpdateSystem]
sealed class ClickSystem : JobComponentSystem
{
    EntityArchetype entityArchetype;
    ComponentGroup g;
    protected override void OnCreateManager(int capacity)
    {
        var componentTypes = new ComponentType[] { ComponentType.ReadOnly<Count>() };
        entityArchetype = EntityManager.CreateArchetype(componentTypes);
        g = GetComponentGroup(componentTypes);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (Input.GetMouseButton(0))
            EntityManager.CreateEntity(entityArchetype);
        else if (Input.GetMouseButton(1))
        {
            var array = g.GetEntityArray();
            if (array.Length == 0)
                return inputDeps;
            using (var dest = new NativeArray<Entity>(array.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            {
                new CopyEntities
                {
                    Results = dest,
                    Source = array,
                }.Schedule(array.Length, 256, inputDeps).Complete();
                EntityManager.DestroyEntity(dest);
            }
        }
        return inputDeps;
    }
}