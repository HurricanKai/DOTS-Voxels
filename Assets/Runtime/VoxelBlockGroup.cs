using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct VoxelBlockGroup : IComponentData
{
    public int3 Extends;
    public int Spread;
}
