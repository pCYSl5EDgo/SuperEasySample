using System;

using Unity.Mathematics;
using Unity.Entities;

struct Velocity : IComponentData, IEquatable<Velocity>
{
    public Velocity(float3 value) => Value = value;
    public float3 Value;

    public bool Equals(Velocity other) => math.all(Value == other.Value);
}