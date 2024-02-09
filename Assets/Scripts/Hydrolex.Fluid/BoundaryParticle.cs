using System;
using Unity.Entities;

[Serializable]
public struct BoundaryParticle : IComponentData
{
    public float Radius;
    public float Mass;
    public float RestDensity;
}