using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(4)]
public struct DanceMove : IBufferElementData
{
    public float3 Velocity;
    public float Duration;
}

public struct StartTime : IComponentData
{
    public float Value;
}