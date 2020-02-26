using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Rendering;
using R = Unity.Mathematics.Random;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class VoxelBlockGroupBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public int XExtends;
    public int YExtends;
    public int ZExtends;
    public int Spread;
    
    

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var extends = int3(XExtends, YExtends, ZExtends);
        var translation = float3(transform.position);

        var entityManager = conversionSystem.DstEntityManager;
        
        var archetype = entityManager.CreateArchetype(ComponentType.ReadWrite<VoxelColor>(),
            ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<LocalToWorld>(),
            ComponentType.ReadWrite<RandomVelocity>(), ComponentType.ReadWrite<Velocity>(),
            ComponentType.ReadWrite<Scale>());
        var r = new Random();
        r.InitState();

        var array = new NativeArray<Entity>(extends.x * extends.y * extends.z, Allocator.Temp);
        entityManager.CreateEntity(archetype, array);

        for (int x = 0; x < extends.x; x++)
        for (int z = 0; z < extends.z; z++)
        for (int y = 0; y < extends.y; y++)
        {
            var p = translation + (int3(x, y, z) * Spread);
            var e = array[x + extends.y * (y + extends.z * z)];
            entityManager.SetComponentData(e, new Translation {Value = p});
            entityManager.SetComponentData(e,
                new RandomVelocity {Speed = r.NextFloat(0f, 5f), Random = new Random(r.NextUInt())});
            entityManager.SetComponentData(e, new Scale
            {
                Value = r.NextFloat(0.5f, 10)
            });
        }

        entityManager.DestroyEntity(entity);
        array.Dispose();
    }
}