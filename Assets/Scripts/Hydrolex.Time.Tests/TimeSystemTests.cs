using System.Collections;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;

[TestFixture]
public class TimeSystemTests
{
    public static IEnumerable TimeTestCases
    {
        get
        {
            yield return new TestCaseData(6.0f, 0.01f)
                .SetName("Test regular data: 6.00 -> 6.01")
                .Returns(6.01f);

            yield return new TestCaseData(24.0f, 0.01f)
                .SetName("Test edge data: 24.00 -> 0.00")
                .Returns(0.00f);
        }
    }

    [Test, TestCaseSource(nameof(TimeTestCases))]
    public float UpdateTest(float time, float progression)
    {
        using var world = new World("Test world");

        var entity = world.EntityManager.CreateEntity(typeof(Time));
        world.EntityManager.SetComponentData(entity, new Time
        {
            Timeline = time,
            TimeProgression = progression
        });

        var timeSystem = world.GetOrCreateSystem<TimeSystem>();

        world.SetTime(new TimeData(10.0f, 1.0f));

        timeSystem.Update(world.Unmanaged);

        world.EntityManager.CompleteAllTrackedJobs();

        return world.EntityManager.GetComponentData<Time>(entity).Timeline;
    }
}