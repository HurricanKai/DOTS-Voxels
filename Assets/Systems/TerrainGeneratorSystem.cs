using System.Xml;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class TerrainGeneratorSystem : JobComponentSystem
{
    private EntityArchetype _archetype;
    private EndInitializationEntityCommandBufferSystem _barrier;

    protected override void OnCreate()
    {
        base.OnCreate();
        _archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<VoxelColor>(), ComponentType.ReadWrite<LocalToWorld>());
        _barrier = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
    }

    private struct J : IJobParallelFor
    {
        public int xMax;
        public int yMax;
        public EntityCommandBuffer.Concurrent Buffer;
        public EntityArchetype Archetype;
        public void Execute(int i)
        {
            var pos = Unpack(i, xMax, yMax);
            var c = noise.cnoise(float2(pos.xz) * .1f);
            if (c * yMax > pos.y)
            {
                var entity = Buffer.CreateEntity(i, Archetype);
                Buffer.SetComponent(i, entity, new Translation
                {
                    Value = pos
                });
                Buffer.SetComponent(i, entity, new VoxelColor
                {
                    Value = Color.HSVToRGB(.3f,  1f, c, true)
                });
            }
        }
    }

    private static int3 Unpack(int i, int xMax, int yMax)
    {
        int z = i / (xMax * yMax);
        i -= (z * xMax * yMax);
        int y = i / xMax;
        int x = i % xMax;
        return int3(x, y, z);
    }

    private static int Pack(int3 i, int xMax, int yMax)
    {
        return (i.z * xMax * yMax) + (i.y * xMax) + i.x;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var handle = inputDeps;
        Entities.WithStructuralChanges().WithoutBurst().ForEach((Entity entity, in TerrainGenerator g) =>
        {
            for (int x = 0; x < g.X; x++)
            {
                handle = JobHandle.CombineDependencies(new J {xMax = g.X, yMax = g.Y, Archetype = _archetype, Buffer = _barrier.CreateCommandBuffer().ToConcurrent() }.Schedule(g.X * g.Y * g.Z, 128, handle),
                    handle);
            }
            EntityManager.DestroyEntity(entity);
        }).Run();
        _barrier.AddJobHandleForProducer(handle);
        return handle;
    }
}