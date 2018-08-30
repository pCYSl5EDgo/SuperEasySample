using UnityEngine;
using UnityEngine.Rendering;

using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;

[RequireComponent(typeof(Camera))]
sealed class Manager_MovingCubes : MonoBehaviour
{
    [SerializeField] Mesh cube;
    [SerializeField] Material cubeMaterial;
    [SerializeField] Texture2D cubeSurfaceTexture;


    void Start()
    {
        World.Active = new World("move cube");
        World.Active.CreateManager(typeof(EndFrameTransformSystem));
        World.Active.CreateManager<MeshInstanceRendererSystem>().ActiveCamera = GetComponent<Camera>();
        World.Active.CreateManager(typeof(MoveSystem));
        World.Active.CreateManager(typeof(ClickSpawnCube), new MeshInstanceRenderer
        {
            castShadows = ShadowCastingMode.On,
            material = this.cubeMaterial = new Material(cubeMaterial)
            {
                enableInstancing = true,
                mainTexture = cubeSurfaceTexture
            },
            mesh = cube,
            receiveShadows = true,
            subMesh = 0,
        });
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
    }
}