using Unity.Entities;

[GenerateAuthoringComponent]
public struct TerrainGenerator : IComponentData
{
    public int X;
    public int Y;
    public int Z;
}