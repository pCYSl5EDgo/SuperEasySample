#if (NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)

using System.Collections.Generic;

namespace Unity.Properties
{
    public interface IPropertyBag
    {
        int PropertyCount { get; }
        IEnumerable<IProperty> Properties { get; }
        IProperty FindProperty(string name);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)