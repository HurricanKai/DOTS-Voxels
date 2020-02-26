using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

[UpdateInGroup(typeof(InputSystemGroup))]
public class FreeCameraSystem : JobComponentSystem
{
    private Inputs _inputs;

    protected override void OnCreate()
    {
        _inputs = new Inputs();
        _inputs.Default.Enable();
    }

    protected override void OnDestroy()
    {
        _inputs.Default.Disable();
        _inputs.Dispose();
    }

    [BurstCompile]
    private struct J : IJobForEach<Translation, Rotation, LocalToWorld, FreeCamera>
    {
        public float2 Movement;
        public float2 Look;
        public bool Boost;
        
        public void Execute(ref Translation translation, ref Rotation rotation, [ReadOnly] ref LocalToWorld localToWorld, [ReadOnly] ref FreeCamera f)
        {
            Movement = Movement * f.Speed;
            
            if (Boost)
                Movement *= f.Boost;
            
            translation.Value += localToWorld.Forward * Movement.y;
            translation.Value += localToWorld.Right * Movement.x;

            Look = Look * f.Sensitivity;
            quaternion orientation = rotation.Value;
            orientation = math.normalize(math.mul(quaternion.AxisAngle(math.float3(0, 1, 0), Look.x), orientation));
            orientation =
                math.normalize(math.mul(quaternion.AxisAngle(math.mul(orientation, math.float3(1, 0, 0)), -Look.y),
                    orientation));
            rotation.Value = orientation;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Profiler.BeginSample("Read Input");
        var active = _inputs.Default.Active.ReadValue<float>();

        if (active == 0)
        {
            Profiler.EndSample();
            return inputDeps;
        }

        float2 movement = _inputs.Default.Movement.ReadValue<Vector2>();
        float2 look = _inputs.Default.Look.ReadValue<Vector2>();

        var boost = _inputs.Default.Boost.ReadValue<float>() != 0;
        Profiler.EndSample();
        return new J
        {
            Movement = movement,
            Look = look,
            Boost = boost
        }.Schedule(this, inputDeps);
    }
}