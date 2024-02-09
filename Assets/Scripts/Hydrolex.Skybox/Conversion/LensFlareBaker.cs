using Unity.Entities;
using UnityEngine.Rendering;

public class LensFlareBaker : Baker<LensFlareComponentSRP>
{
    public override void Bake(LensFlareComponentSRP authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, authoring);
    }
}