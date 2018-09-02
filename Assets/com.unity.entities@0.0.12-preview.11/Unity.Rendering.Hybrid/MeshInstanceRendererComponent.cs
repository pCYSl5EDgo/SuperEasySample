using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    /// <summary>
    /// Render Mesh with Material (must be instanced material) by object to world matrix.
    /// Specified by TransformMatrix associated with Entity.
    /// </summary>
    [Serializable]
    public struct MeshInstanceRenderer : Unity.Entities.ISharedComponentData
    {
        public UnityEngine.Mesh mesh;
        public UnityEngine.Material material;
        public int subMesh;

        public UnityEngine.Rendering.ShadowCastingMode castShadows;
        public bool receiveShadows;
    }

    public class MeshInstanceRendererComponent : SharedComponentDataWrapper<MeshInstanceRenderer> { }
}
