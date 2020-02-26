
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using static Unity.Mathematics.math;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class VoxSRP : RenderPipeline
{
    private Mesh _voxMesh;
    private Material _voxMat;
    private readonly CommandBuffer _cb;
    private readonly MaterialPropertyBlock _matPropBlock;
    private int _colorProp;
    // private int _scaleBufferProp;
    private int _positionBufferProp;
    private ComputeBufferPool _colorBufferPool;
    private ComputeBufferPool _positionBufferPool;
    // private ComputeBufferPool _scaleBufferPool;
    public const int BatchSize = 512;

    public VoxSRP()
    {
        _cb = new CommandBuffer();
        _matPropBlock = new MaterialPropertyBlock();
        _colorProp = Shader.PropertyToID("colorBuffer");
        _positionBufferProp = Shader.PropertyToID("positionBuffer");
        _positionBufferPool = new ComputeBufferPool(BatchSize, sizeof(float) * 4 * 4, ComputeBufferType.Structured, "Position Buffer");
        _colorBufferPool = new ComputeBufferPool(BatchSize, sizeof(float) * 4, ComputeBufferType.Structured, "Color Buffer");
        _voxMesh = CreateMesh();
        _voxMat = GraphicsSettings.renderPipelineAsset?.defaultMaterial;
        
        #if UNITY_EDITOR
        SupportedRenderingFeatures.active = new SupportedRenderingFeatures
        {
            defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.None,
            editableMaterialRenderQueue = true,
            enlighten = false,
            lightmapBakeTypes = 0,
            lightmapsModes = 0,
            lightProbeProxyVolumes = false,
            mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.None,
            motionVectors = false,
            overridesEnvironmentLighting = true,
            overridesFog = true,
            overridesLODBias = true,
            overridesMaximumLODLevel = true,
            overridesOtherLightingSettings = true,
            receiveShadows = false,
            reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
            reflectionProbes = false,
            rendererPriority = false
        };
        #endif
    }
    
    private Mesh CreateMesh()
    {
        Vector3[] vertices =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
        };

        int[] triangles =
        {
            0, 2, 1, //face front
            0, 3, 2,
            2, 3, 4, //face top
            2, 4, 5,
            1, 2, 5, //face right
            1, 5, 6,
            0, 7, 4, //face left
            0, 4, 3,
            5, 4, 7, //face back
            5, 7, 6,
            0, 6, 7, //face bottom
            0, 1, 6
        };

        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.Optimize();
        return mesh;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _cb.Dispose();
        _colorBufferPool.Dispose();
        _positionBufferPool.Dispose();
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        context.InvokeOnRenderObjectCallback();
        
        BeginFrameRendering(context, cameras);
        
        _positionBufferPool.Swap();
        _colorBufferPool.Swap();
        
        foreach (var camera in cameras)
        {
            Profiler.BeginSample("Camera " + camera.name);
            context.SetupCameraProperties(camera);
            Render(context, camera);
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            Profiler.EndSample();
        }
        
        context.Submit();

        EndFrameRendering(context, cameras);
    }

    private void Render(ScriptableRenderContext ctx, Camera camera)
    {
        BeginCameraRendering(ctx, camera);
        _cb.Clear();
        _cb.ClearRenderTarget(true, true, camera.backgroundColor);

        foreach (var world in World.AllWorlds)
        {
            Profiler.BeginSample("World " + world.Name);
            var vrs = world.GetExistingSystem<VoxelRenderSystem>();

            if (vrs != null)
            {
                _cb.BeginSample("World " + world.Name);
                Profiler.BeginSample("World " + world.Name);
                var positionBuffer = _positionBufferPool.Rent();
                var colorBuffer = _colorBufferPool.Rent();
                
                vrs.LastJob.Complete();
                while (vrs._batchQueue.TryDequeue(out var batch))
                {
                    Profiler.BeginSample("Batch");
                    Profiler.BeginSample("Copy");
                    {
                        colorBuffer.SetData(vrs._lastColors, batch.GlobalIndex, 0, batch.Length);
                        positionBuffer.SetData(vrs._lastMatrices, batch.GlobalIndex, 0, batch.Length);
                    }
                    Profiler.EndSample();
                    Profiler.BeginSample("Submitting");
                    _matPropBlock.Clear();
                    _matPropBlock.SetBuffer(_colorProp, colorBuffer);
                    _matPropBlock.SetBuffer(_positionBufferProp, positionBuffer);
			
                    _cb.DrawMeshInstancedProcedural(_voxMesh, 0, _voxMat, -1, batch.Length, _matPropBlock);
                    Profiler.EndSample();
                    Profiler.BeginSample("Resetting");
                    positionBuffer = _positionBufferPool.Rent();
                    colorBuffer = _colorBufferPool.Rent();
                    Profiler.EndSample();
                    Profiler.EndSample();
                }
                Profiler.EndSample();
                _cb.EndSample("World " + world.Name);
            }
            Profiler.EndSample();
        }

        ctx.ExecuteCommandBuffer(_cb);
        
        EndCameraRendering(ctx, camera);
    }
}