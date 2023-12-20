using Unity.Entities;
using UnityEngine;

public class BoundaryParticleAuthoring : MonoBehaviour
{
    public BoundaryParticle BoundaryParticle = default;

    private class BoundaryParticleBaker : Baker<BoundaryParticleAuthoring>
    {
        public override void Bake(BoundaryParticleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, authoring.BoundaryParticle);
        }
    }
}