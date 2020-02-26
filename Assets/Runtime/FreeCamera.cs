using Unity.Entities;

[GenerateAuthoringComponent]
public struct FreeCamera : IComponentData
{
    public float Speed;
    public float Sensitivity;
    public float Boost;
}