using Unity.Entities;
using UnityEngine;

public class FluidParticleAuthoring : MonoBehaviour
{
    public FluidParticle FluidParticle = default;

    private class FluidParticleBaker : Baker<FluidParticleAuthoring>
    {
        public override void Bake(FluidParticleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, authoring.FluidParticle);
        }
    }
}