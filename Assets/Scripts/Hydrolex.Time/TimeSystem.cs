using Unity.Burst;
using Unity.Entities;

public partial struct TimeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Time>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var time in SystemAPI.Query<RefRW<Time>>())
        {
            time.ValueRW.Timeline += time.ValueRW.TimeProgression * SystemAPI.Time.DeltaTime;

            if (time.ValueRW.Timeline > 24.0f) time.ValueRW.Timeline = 0.0f;
        }
    }
}