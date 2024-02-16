using Unity.Entities;
using UnityEngine;

public class BoundaryAuthoring : MonoBehaviour
{
    public GameObject Prefab;

    private class BoundaryBaker : Baker<BoundaryAuthoring>
    {
        public override void Bake(BoundaryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Boundary {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}