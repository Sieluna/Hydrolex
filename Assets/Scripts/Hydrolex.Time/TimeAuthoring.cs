using Unity.Entities;
using UnityEngine;

public class TimeAuthoring : MonoBehaviour
{
    [Range(0f, 24f)] public float Timeline = 6.8f;
    public float DayCycleInMinutes = 30f;

    private class TimeBaker : Baker<TimeAuthoring>
    {
        public override void Bake(TimeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new Time
            {
                Timeline = authoring.Timeline,
                TimeProgression = GetTimeProgression(authoring.DayCycleInMinutes)
            });
        }

        private static float GetTimeProgression(float dayCycleInMinutes)
        {
            if (dayCycleInMinutes > 0.0f)
                return (24.0f / 60.0f) / dayCycleInMinutes;

            return 0.0f;
        }
    }
}