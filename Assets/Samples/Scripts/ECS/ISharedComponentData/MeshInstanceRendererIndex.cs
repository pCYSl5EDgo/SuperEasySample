using Unity.Entities;

using System;

public sealed partial class MeshInstanceRendererInstancedIndirectSystem
{
    public readonly struct MeshInstanceRendererIndex : ISharedComponentData, IEquatable<MeshInstanceRendererIndex>, IComparable<MeshInstanceRendererIndex>
    {
        public MeshInstanceRendererIndex(uint value) => Value = value == 0 ? throw new ArgumentOutOfRangeException() : value;
        public MeshInstanceRendererIndex(int value) => Value = value <= 0 ? throw new ArgumentOutOfRangeException() : (uint)value;
        public readonly uint Value;
        public bool Equals(MeshInstanceRendererIndex other) => Value == other.Value;
        public bool Equals(in MeshInstanceRendererIndex other) => Value == other.Value;
        public override int GetHashCode() => (int)Value;
        public override bool Equals(object obj) => obj != null && ((MeshInstanceRendererIndex)obj).Value == Value;
        public int CompareTo(MeshInstanceRendererIndex other) => Value.CompareTo(other.Value);
        public int CompareTo(in MeshInstanceRendererIndex other) => Value.CompareTo(other.Value);
        public static bool operator ==(MeshInstanceRendererIndex left, MeshInstanceRendererIndex right) => left.Value == right.Value;
        public static bool operator !=(MeshInstanceRendererIndex left, MeshInstanceRendererIndex right) => left.Value != right.Value;
    }
}