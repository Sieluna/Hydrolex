using System;
using Unity.Entities;

[Serializable]
public struct FluidParticle : IComponentData
{
    public float Radius;
    public float RestDensity;
    public float Viscosity;
    public float GasConstant;
}