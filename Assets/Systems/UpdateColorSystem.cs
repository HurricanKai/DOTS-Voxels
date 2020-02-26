using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/*public class UpdateColorSystem : JobComponentSystem
{
    [BurstCompile]
    private struct J : IJobForEach<VoxelColor, Translation>
    {
        public void Execute(ref VoxelColor c0, [ReadOnly] ref Translation c1)
        {
            // var c = noise.cellular2x2x2(c1.Value * .03f);
            // c0.Value = Color.HSVToRGB(c.x, c.y, 1f);
            c0.Value = Color.HSVToRGB(noise.snoise(c1.Value * 0.001f), 1f, 1f);
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps) => new J().Schedule(this, inputDeps);
}*/