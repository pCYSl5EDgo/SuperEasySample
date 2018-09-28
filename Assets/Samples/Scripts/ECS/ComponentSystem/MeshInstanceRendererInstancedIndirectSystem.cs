using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

using System;

using UnityEngine;

public sealed partial class MeshInstanceRendererInstancedIndirectSystem : ComponentSystem
{
    public MeshInstanceRendererInstancedIndirectSystem(Camera activeCamera, MeshInstanceRenderer[] renderers, ComputeShader shader)
    {
        if ((this.activeCamera = activeCamera) == null) throw new ArgumentNullException("Camera must not be null!");
        if ((this.renderers = renderers) == null) throw new ArgumentNullException("Renderers must not be null!");
        if ((this.shader = shader) == null) throw new ArgumentNullException();
        if (renderers.Length == 0) throw new ArgumentException("Length of the renderers must not be 0!");
        buffers_Position = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        buffers_Position_Rotation = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        buffers_Position_Scale = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        buffers_Position_Scale_Rotation = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        buffers_Rotation = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        buffers_Scale = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        buffers_Scale_Rotation = new(ComputeBuffer, ComputeBuffer, ComputeBuffer, ComputeBuffer, int, int)[renderers.Length];
        var tmpArgs = new uint[5];
        for (int i = 0; i < buffers_Position.Length; i++)
        {
            ref var renderer = ref renderers[i];
            tmpArgs[0] = renderer.mesh.GetIndexCount(renderer.subMesh);
            (buffers_Position[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
            (buffers_Scale[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
            (buffers_Position_Scale[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
            (buffers_Rotation[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
            (buffers_Position_Rotation[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
            (buffers_Scale_Rotation[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
            (buffers_Position_Scale_Rotation[i].args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments)).SetData(tmpArgs);
        }
    }

    private readonly Camera activeCamera;
    private readonly ComputeShader shader;
    private readonly EntityArchetypeQuery query = new EntityArchetypeQuery
    {
        None = Array.Empty<ComponentType>(),
        Any = new[] { ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Scale>(), ComponentType.ReadOnly<Rotation>() },
        All = new[] { ComponentType.ReadOnly<MeshInstanceRendererIndex>() },
    };
    private readonly MeshInstanceRenderer[] renderers;
    private readonly NativeList<EntityArchetype> foundArchetypes = new NativeList<EntityArchetype>(1024, Allocator.Persistent);
    private static readonly int[] argsLength = new int[1];
    private static readonly Bounds bounds = new Bounds(Vector3.zero, (float3)10000);
    private static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    private static readonly int shaderProperty_PositionBuffer = Shader.PropertyToID("_PositionBuffer");
    private static readonly int shaderProperty_ScaleBuffer = Shader.PropertyToID("_ScaleBuffer");
    private static readonly int shaderProperty_RotationBuffer = Shader.PropertyToID("_RotationBuffer");
    private static readonly int shaderProperty_TransformMatrixBuffer = Shader.PropertyToID("_TransformMatrixBuffer");
    private static readonly int shaderProperty_ResultBuffer = Shader.PropertyToID("_ResultBuffer");
    private readonly (ComputeBuffer args, ComputeBuffer positions, ComputeBuffer transforms, int count, int maxCount)[] buffers_Position;
    private readonly (ComputeBuffer args, ComputeBuffer scales, ComputeBuffer transforms, int count, int maxCount)[] buffers_Scale;
    private readonly (ComputeBuffer args, ComputeBuffer positions, ComputeBuffer scales, ComputeBuffer transforms, int count, int maxCount)[] buffers_Position_Scale;
    private readonly (ComputeBuffer args, ComputeBuffer rotations, ComputeBuffer transforms, int count, int maxCount)[] buffers_Rotation;
    private readonly (ComputeBuffer args, ComputeBuffer positions, ComputeBuffer rotations, ComputeBuffer transforms, int count, int maxCount)[] buffers_Position_Rotation;
    private readonly (ComputeBuffer args, ComputeBuffer scales, ComputeBuffer rotations, ComputeBuffer transforms, int count, int maxCount)[] buffers_Scale_Rotation;
    private readonly (ComputeBuffer args, ComputeBuffer positions, ComputeBuffer scales, ComputeBuffer rotations, ComputeBuffer transforms, int count, int maxCount)[] buffers_Position_Scale_Rotation;

    private static readonly ProfilerMarker profileUpdateGatherChunks = new ProfilerMarker("Custom GatherChunks");
    private static readonly ProfilerMarker profileUpdateDraw = new ProfilerMarker("Custom Draw");

    private int capacity;

    protected override void OnCreateManager() => this.capacity = GetPow2Container(1024);

    private static int GetPow2Container(int capacity)
    {
        if (capacity == 0) return 1;
        if (((capacity - 1) & capacity) == 0) return capacity;
        var v = (uint)capacity;
        v |= (v >> 1);
        v |= (v >> 2);
        v |= (v >> 4);
        v |= (v >> 8);
        v |= (v >> 16);
        return 1 << math.countbits(v);
    }

    protected override void OnDestroyManager()
    {
        for (int i = 0; i < buffers_Position.Length; i++)
        {
            ref var bp = ref buffers_Position[i];
            bp.args.Release();
            if (bp.positions != null)
            {
                bp.positions.Release();
                bp.transforms.Release();
            }
            ref var bs = ref buffers_Scale[i];
            bs.args.Release();
            if (bs.scales != null)
            {
                bs.scales.Release();
                bs.transforms.Release();
            }
            ref var bps = ref buffers_Position_Scale[i];
            bps.args.Release();
            if (bps.positions != null)
            {
                bps.positions.Release();
                bps.scales.Release();
                bps.transforms.Release();
            }
            ref var br = ref buffers_Rotation[i];
            br.args.Release();
            if (br.rotations != null)
            {
                br.rotations.Release();
                br.transforms.Release();
            }
            ref var bpr = ref buffers_Position_Rotation[i];
            bpr.args.Release();
            if (bpr.positions != null)
            {
                bpr.positions.Release();
                bpr.rotations.Release();
                bpr.transforms.Release();
            }
            ref var bsr = ref buffers_Scale_Rotation[i];
            bsr.args.Release();
            if (bsr.scales != null)
            {
                bsr.scales.Release();
                bsr.rotations.Release();
                bsr.transforms.Release();
            }
            ref var bpsr = ref buffers_Position_Scale_Rotation[i];
            bpsr.args.Release();
            if (bpsr.positions != null)
            {
                bpsr.positions.Release();
                bpsr.scales.Release();
                bpsr.rotations.Release();
                bpsr.transforms.Release();
            }
        }
        foundArchetypes.Dispose();
    }

    protected override void OnUpdate()
    {
        InitializeOnUpdate();
        GatherChunks();
        profileUpdateGatherChunks.End();
        profileUpdateDraw.Begin();
        Draw();
        profileUpdateDraw.End();
    }

    private void InitializeOnUpdate()
    {
        EntityManager.AddMatchingArchetypes(query, foundArchetypes);
        for (int i = 0; i < buffers_Position.Length; i++)
        {
            buffers_Position[i].count = 0;
            buffers_Position_Rotation[i].count = 0;
            buffers_Position_Scale[i].count = 0;
            buffers_Position_Scale_Rotation[i].count = 0;
            buffers_Rotation[i].count = 0;
            buffers_Scale[i].count = 0;
            buffers_Scale_Rotation[i].count = 0;
        }
    }

    private void ReAllocate<T>(ref ComputeBuffer buffer, int exSize) where T : struct
    {
        var _ = new T[exSize];
        buffer.GetData(_, 0, 0, exSize);
        buffer.Release();
        buffer = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<T>());
        buffer.SetData(_);
    }

    private void GatherChunks()
    {
        var acct_Position = GetArchetypeChunkComponentType<Position>(true);
        var acct_Scale = GetArchetypeChunkComponentType<Scale>(true);
        var acct_Rotation = GetArchetypeChunkComponentType<Rotation>(true);
        var acsct_MeshInstanceRendererIndex = GetArchetypeChunkSharedComponentType<MeshInstanceRendererIndex>();
        using (var chunks = EntityManager.CreateArchetypeChunkArray(foundArchetypes, Allocator.Temp))
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var sharedIndex = chunks[i].GetSharedComponentIndex(acsct_MeshInstanceRendererIndex);
                var rendererIndex = EntityManager.GetSharedComponentData<MeshInstanceRendererIndex>(sharedIndex).Value;
                if (rendererIndex == 0) continue;

                var pos = chunks[i].GetNativeArray(acct_Position);
                var scl = chunks[i].GetNativeArray(acct_Scale);
                var rot = chunks[i].GetNativeArray(acct_Rotation);

                int length = Math.Max(Math.Max(pos.Length, scl.Length), rot.Length);
                if (length > capacity)
                    capacity = GetPow2Container(length);
                switch (-1 + (pos.Length > 0 ? 1 : 0) + (scl.Length > 0 ? 2 : 0) + (rot.Length > 0 ? 4 : 0))
                {
                    case 0: // Position
                        Initialize_Position(rendererIndex, pos, length);
                        continue;
                    case 1: // Scale
                        Initialize_Scale(rendererIndex, scl, length);
                        continue;
                    case 2: // Position | Scale
                        Initialize_Position_Scale(rendererIndex, pos, scl, length);
                        continue;
                    case 3: // Rotation
                        Initialize_Rotation(rendererIndex, rot, length);
                        continue;
                    case 4: // Position | Rotation
                        Initialize_Position_Rotation(rendererIndex, pos, rot, length);
                        continue;
                    case 5: // Scale | Rotation
                        Initialize_Scale_Rotation(rendererIndex, scl, rot, length);
                        continue;
                    case 6: // Position | Scale | Rotation
                        Initialize_Position_Scale_Rotation(rendererIndex, pos, scl, rot, length);
                        continue;
                    default: continue;
                }
            }
        }
    }

    private void Initialize_Position_Scale_Rotation(uint rendererIndex, NativeArray<Position> pos, NativeArray<Scale> scl, NativeArray<Rotation> rot, int length)
    {
        ref var buffer = ref buffers_Position_Scale_Rotation[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.positions = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.scales = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.rotations = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float3>(ref buffer.positions, buffer.count);
            ReAllocate<float3>(ref buffer.scales, buffer.count);
            ReAllocate<float4>(ref buffer.rotations, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.positions.SetData(pos, 0, buffer.count, length);
        buffer.scales.SetData(scl, 0, buffer.count, length);
        buffer.rotations.SetData(rot, 0, buffer.count, length);
        buffer.count += length;
    }

    private void Initialize_Scale_Rotation(uint rendererIndex, NativeArray<Scale> scl, NativeArray<Rotation> rot, int length)
    {
        ref var buffer = ref buffers_Scale_Rotation[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.scales = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.rotations = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float3>(ref buffer.scales, buffer.count);
            ReAllocate<float4>(ref buffer.rotations, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.scales.SetData(scl, 0, buffer.count, length);
        buffer.rotations.SetData(rot, 0, buffer.count, length);
        buffer.count += length;
    }

    private void Initialize_Position_Rotation(uint rendererIndex, NativeArray<Position> pos, NativeArray<Rotation> rot, int length)
    {
        ref var buffer = ref buffers_Position_Rotation[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.positions = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.rotations = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float3>(ref buffer.positions, buffer.count);
            ReAllocate<float4>(ref buffer.rotations, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.positions.SetData(pos, 0, buffer.count, length);
        buffer.rotations.SetData(rot, 0, buffer.count, length);
        buffer.count += length;
    }

    private void Initialize_Rotation(uint rendererIndex, NativeArray<Rotation> rot, int length)
    {
        ref var buffer = ref buffers_Rotation[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.rotations = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float4>(ref buffer.rotations, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.rotations.SetData(rot, 0, buffer.count, length);
        buffer.count += length;
    }

    private void Initialize_Position_Scale(uint rendererIndex, NativeArray<Position> pos, NativeArray<Scale> scl, int length)
    {
        ref var buffer = ref buffers_Position_Scale[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.positions = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.scales = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float3>(ref buffer.positions, buffer.count);
            ReAllocate<float3>(ref buffer.scales, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.positions.SetData(pos, 0, buffer.count, length);
        buffer.scales.SetData(scl, 0, buffer.count, length);
        buffer.count += length;
    }

    private void Initialize_Scale(uint rendererIndex, NativeArray<Scale> scl, int length)
    {
        ref var buffer = ref buffers_Scale[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.scales = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float3>(ref buffer.scales, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.scales.SetData(scl, 0, buffer.count, length);
        buffer.count += length;
    }

    private void Initialize_Position(uint rendererIndex, NativeArray<Position> pos, int length)
    {
        ref var buffer = ref buffers_Position[rendererIndex - 1];
        if (buffer.transforms == null)
        {
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            buffer.positions = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float3>());
            buffer.maxCount = capacity;
        }
        else if (buffer.count + length > buffer.maxCount)
        {
            capacity = Math.Max(capacity, GetPow2Container(buffer.count + length));
            buffer.transforms.Release();
            buffer.transforms = new ComputeBuffer(capacity, UnsafeUtility.SizeOf<float4x4>());
            ReAllocate<float3>(ref buffer.positions, buffer.count);
            buffer.maxCount = capacity;
        }
        buffer.positions.SetData(pos, 0, buffer.count, length);
        buffer.count += length;
    }

    private void DrawInternal0(ComputeBuffer positions) => shader.SetBuffer(0, shaderProperty_PositionBuffer, positions);
    private void DrawInternal1(ComputeBuffer scales) => shader.SetBuffer(1, shaderProperty_ScaleBuffer, scales);
    private void DrawInternal2(ComputeBuffer positions, ComputeBuffer scales)
    {
        shader.SetBuffer(2, shaderProperty_PositionBuffer, positions);
        shader.SetBuffer(2, shaderProperty_ScaleBuffer, scales);
    }
    private void DrawInternal3(ComputeBuffer rotations) => shader.SetBuffer(3, shaderProperty_RotationBuffer, rotations);
    private void DrawInternal4(ComputeBuffer positions, ComputeBuffer rotations)
    {
        shader.SetBuffer(4, shaderProperty_PositionBuffer, positions);
        shader.SetBuffer(4, shaderProperty_RotationBuffer, rotations);
    }
    private void DrawInternal5(ComputeBuffer scales, ComputeBuffer rotations)
    {
        shader.SetBuffer(5, shaderProperty_ScaleBuffer, scales);
        shader.SetBuffer(5, shaderProperty_RotationBuffer, rotations);
    }
    private void DrawInternal6(ComputeBuffer positions, ComputeBuffer scales, ComputeBuffer rotations)
    {
        shader.SetBuffer(6, shaderProperty_PositionBuffer, positions);
        shader.SetBuffer(6, shaderProperty_ScaleBuffer, scales);
        shader.SetBuffer(6, shaderProperty_RotationBuffer, rotations);
    }

    private void DrawDispatch(int index, int count, ComputeBuffer args, ComputeBuffer transforms, int @case)
    {
        shader.SetBuffer(@case, shaderProperty_ResultBuffer, transforms);
        int threadGroupsX = (int)(((uint)count) >> _SHIFT_);
        if (threadGroupsX << _SHIFT_ == count)
            shader.Dispatch(@case, threadGroupsX, 1, 1);
        else shader.Dispatch(@case, threadGroupsX + 1, 1, 1);
        ref var renderer = ref renderers[index];
        Graphics.DrawMeshInstancedIndirect(renderer.mesh, renderer.subMesh, renderer.material, bounds, args, 0, block, renderer.castShadows, renderer.receiveShadows, 0, activeCamera);
    }

    private static void PreDraw(int count, ComputeBuffer args, ComputeBuffer transforms)
    {
        argsLength[0] = count;
        args.SetData(argsLength, 0, 1, 1);
        block.SetBuffer(shaderProperty_TransformMatrixBuffer, transforms);
    }

    private const int _SHIFT_ = 10;
    private void Draw()
    {
        ComputeBuffer args, transforms;
        int count;
        for (int i = 0; i < buffers_Position.Length; i++)
        {
            if ((count = buffers_Position[i].count) == 0) goto SCALE;
            PreDraw(count, args = buffers_Position[i].args, transforms = buffers_Position[i].transforms);
            DrawInternal0(buffers_Position[i].positions);
            DrawDispatch(i, count, args, transforms, 0);
        SCALE:
            if ((count = buffers_Scale[i].count) == 0) goto POSITION_SCALE;
            PreDraw(count, args = buffers_Scale[i].args, transforms = buffers_Scale[i].transforms);
            DrawInternal1(buffers_Scale[i].scales);
            DrawDispatch(i, count, args, transforms, 1);
        POSITION_SCALE:
            if ((count = buffers_Position_Scale[i].count) == 0) goto ROTATION;
            PreDraw(count, args = buffers_Position_Scale[i].args, transforms = buffers_Position_Scale[i].transforms);
            DrawInternal2(buffers_Position_Scale[i].positions, buffers_Position_Scale[i].scales);
            DrawDispatch(i, count, args, transforms, 2);
        ROTATION:
            if ((count = buffers_Rotation[i].count) == 0) goto POSITION_ROTATION;
            PreDraw(count, args = buffers_Rotation[i].args, transforms = buffers_Rotation[i].transforms);
            DrawInternal3(buffers_Rotation[i].rotations);
            DrawDispatch(i, count, args, transforms, 3);
        POSITION_ROTATION:
            if ((count = buffers_Position_Rotation[i].count) == 0) goto SCALE_ROTATION;
            PreDraw(count, args = buffers_Position_Rotation[i].args, transforms = buffers_Position_Rotation[i].transforms);
            DrawInternal4(buffers_Position_Rotation[i].positions, buffers_Position_Rotation[i].rotations);
            DrawDispatch(i, count, args, transforms, 4);
        SCALE_ROTATION:
            if ((count = buffers_Scale_Rotation[i].count) == 0) goto POSITION_SCALE_ROTATION;
            PreDraw(count, args = buffers_Scale_Rotation[i].args, transforms = buffers_Scale_Rotation[i].transforms);
            DrawInternal5(buffers_Scale_Rotation[i].scales, buffers_Scale_Rotation[i].rotations);
            DrawDispatch(i, count, args, transforms, 5);
        POSITION_SCALE_ROTATION:
            if ((count = buffers_Position_Scale_Rotation[i].count) == 0) continue;
            PreDraw(count, args = buffers_Position_Scale_Rotation[i].args, transforms = buffers_Position_Scale_Rotation[i].transforms);
            DrawInternal6(buffers_Position_Scale_Rotation[i].positions, buffers_Position_Scale_Rotation[i].scales, buffers_Position_Scale_Rotation[i].rotations);
            DrawDispatch(i, count, args, transforms, 6);
        }
    }
}