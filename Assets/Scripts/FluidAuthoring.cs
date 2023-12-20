using Unity.Entities;
using UnityEngine;

public class FluidAuthoring : MonoBehaviour
{
    public GameObject Prefab;

    private class FluidBaker : Baker<FluidAuthoring>
    {
        public override void Bake(FluidAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Fluid
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}