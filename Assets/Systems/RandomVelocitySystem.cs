using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class RandomVelocitySystem : JobComponentSystem
{
    private Random _random;

    [BurstCompile]
    private struct J : IJobForEach<RandomVelocity, Velocity>
    {
        public void Execute(ref RandomVelocity c0, ref Velocity c1)
        {
            var r = c0.Random;
            c1.Value += r.NextFloat3Direction();
            c1.Value = math.normalize(c1.Value) * c0.Speed;
            c0.Random = r;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) => new J().Schedule(this, inputDeps);
}