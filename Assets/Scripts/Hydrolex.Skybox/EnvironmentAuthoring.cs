using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

public class EnvironmentAuthoring : MonoBehaviour
{
    public GameObject LightTransform;
    public GameObject ReflectionProbeTransform;
    public ReflectionProbeState State = ReflectionProbeState.Off;
    public ReflectionProbeRefreshMode RefreshMode = ReflectionProbeRefreshMode.OnAwake;
    public ReflectionProbeTimeSlicingMode TimeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
    public float ProbeRefreshInterval = 2.0f;
    public float LightIntensity = 1;
    public Color LightColor = new(1f, 0.71f, 0.39f);
    public float AmbientIntensity = 1;
    public Color AmbientSkyColor = new(0.89f, 0.64f, 0.51f);

    private class EnvironmentBaker : Baker<EnvironmentAuthoring>
    {
        public override void Bake(EnvironmentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Environment
            {
                LightTransform = GetEntity(authoring.LightTransform, TransformUsageFlags.Dynamic),
                ReflectionProbeTransform = GetEntity(authoring.ReflectionProbeTransform, TransformUsageFlags.Dynamic),
                State = authoring.State,
                RefreshMode = authoring.RefreshMode,
                TimeSlicingMode = authoring.TimeSlicingMode,
                ProbeRefreshInterval = authoring.ProbeRefreshInterval,
                LightIntensity = authoring.LightIntensity,
                LightColor = authoring.LightColor,
                FlareIntensity = authoring.LightIntensity,
                AmbientIntensity = authoring.AmbientIntensity,
                AmbientSkyColor = authoring.AmbientSkyColor,
                EquatorSkyColor = authoring.AmbientSkyColor,
                GroundSkyColor = authoring.AmbientSkyColor
            });
        }
    }

    private class LensFlareBaker : Baker<LensFlareComponentSRP>
    {
        public override void Bake(LensFlareComponentSRP authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponentObject(entity, authoring);
        }
    }
}