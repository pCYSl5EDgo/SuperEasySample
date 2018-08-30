#if (NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)

namespace Unity.Properties.Codegen
{
    public interface ISchemaBuilder
    {
        void Build(string path, Schema schema);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)