using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;

using UnityEngine;

[UpdateBefore(typeof(EndFrameTransformSystem))]
public class MoveSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(MeshInstanceRenderer))]
    struct Job : IJobProcessComponentData<Position, Velocity>
    {
        public float deltaTime;
        public void Execute(ref Position position, [ReadOnly]ref Velocity velocity) => position.Value += deltaTime * velocity.Value;
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps) => new Job { deltaTime = Time.deltaTime }.Schedule(this, 256, inputDeps);
}