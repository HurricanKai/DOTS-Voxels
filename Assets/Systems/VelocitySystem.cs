using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public class VelocitySystem : JobComponentSystem
{
    [BurstCompile]
    private struct J : IJobForEach<Velocity, Translation>
    {
        public void Execute([ReadOnly] ref Velocity c0, ref Translation c1)
        {
            c1.Value += c0.Value;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) => new J().Schedule(this, inputDeps);
}