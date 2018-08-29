using Unity.Mathematics;

struct Velocity : Unity.Entities.IComponentData, System.IEquatable<Velocity>
{
    public Velocity(float3 value) => Value = value;
    public float3 Value;

    public bool Equals(Velocity other) => math.all(Value == other.Value);
    public bool Equals(in Velocity other) => math.all(Value == other.Value);

    public static implicit operator float3(Velocity origin) => origin.Value;
    public static implicit operator Velocity(float3 origin) => new Velocity(origin);

    public override int GetHashCode()
    {
        var int3 = math.asint(Value);
        return int3.x ^ int3.y ^ int3.z;
    }
    public override bool Equals(object obj) => obj != null && math.all(Value == ((Velocity)obj).Value);
    public static bool operator ==(Velocity left, Velocity right) => math.all(left.Value == right.Value);
    public static bool operator !=(Velocity left, Velocity right) => math.any(left.Value != right.Value);
}