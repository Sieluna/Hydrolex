using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

public enum ReflectionProbeState { On, Off }

public struct Environment : IComponentData
{
    public Entity LightTransform;
    public Entity ReflectionProbeTransform;
    public ReflectionProbeState State;
    public ReflectionProbeRefreshMode RefreshMode;
    public ReflectionProbeTimeSlicingMode TimeSlicingMode;
    public float ProbeRefreshInterval;
    public float TimeSinceLastProbeUpdate;
    public float LightIntensity;
    public Color LightColor;
    public float FlareIntensity;
    public float AmbientIntensity;
    public Color AmbientSkyColor;
    public Color EquatorSkyColor;
    public Color GroundSkyColor;
}