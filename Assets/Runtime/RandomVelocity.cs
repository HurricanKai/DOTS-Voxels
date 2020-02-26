using Unity.Entities;
using Unity.Mathematics;

public struct RandomVelocity : IComponentData
{
    public Random Random;
    public float Speed;
}