using System;
using Unity.Entities;
using UnityEngine;

public struct HybridCamera : ISharedComponentData, IEquatable<HybridCamera>
{
    public Camera Camera;

    public bool Equals(HybridCamera other) => Equals(Camera, other.Camera);

    public override bool Equals(object obj) => obj is HybridCamera other && Equals(other);

    public override int GetHashCode() => (Camera != null ? Camera.GetHashCode() : 0);

    public static bool operator ==(HybridCamera left, HybridCamera right) => left.Equals(right);

    public static bool operator !=(HybridCamera left, HybridCamera right) => !left.Equals(right);
}