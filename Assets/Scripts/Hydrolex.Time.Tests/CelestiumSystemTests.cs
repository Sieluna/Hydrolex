using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[TestFixture]
public class CelestiumSystemTests
{
    public static IEnumerable CelestiumTestCases
    {
        get
        {
            yield return new TestCaseData(new object[]
                {
                    12.0f,
                    new Celestium { SimulationType = CelestiumSimulation.Simple, Latitude = 0, Longitude = 0, Utc = 0 },
                    (new quaternion(0.0f, 0.707f, -0.707f, 0.0f), math.down()),
                    (new quaternion(-0.707f, 0.0f, 0.0f, 0.707f), math.up())
                })
                .SetName("SimpleCenterPositionMidDay");
        }
    }

    [Test, TestCaseSource(nameof(CelestiumTestCases))]
    public void GetSunDirectionTest(float time, Celestium preset, (quaternion, float3) sun, (quaternion, float3) moon)
    {
        if (preset.SimulationType == CelestiumSimulation.Simple)
        {
            var actual = CelestiumSystem.GetChimericalSunDirection(preset, time);

            AssertQuaternionEqual(sun.Item1, actual, 1e-3f, $"expect: {sun.Item2}, actual: {actual}");
        }
    }

    [Test, TestCaseSource(nameof(CelestiumTestCases))]
    public void GetMoonDirectionTest(float time, Celestium preset, (quaternion, float3) sun, (quaternion, float3) moon)
    {
        if (preset.SimulationType == CelestiumSimulation.Simple)
        {
            var actual = CelestiumSystem.GetChimericalMoonDirection(sun.Item2);

            AssertQuaternionEqual(moon.Item1, actual, 1e-3f, $"expect: {moon.Item2}, actual: {actual}");
        }
    }

    [Test, TestCaseSource(nameof(CelestiumTestCases))]
    public void UpdateTest(float time, Celestium preset, (quaternion, float3) sun, (quaternion, float3) moon)
    {
        using var world = new World("Test world");

        var entity = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(Celestium), typeof(Time));
        var moonEntity = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(Parent));
        var sunEntity = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(Parent));

        world.EntityManager.SetComponentData(moonEntity, new Parent { Value = entity });
        world.EntityManager.SetComponentData(sunEntity, new Parent { Value = entity });
        world.EntityManager.SetComponentData(entity, LocalTransform.Identity);
        world.EntityManager.SetComponentData(moonEntity, LocalTransform.Identity);
        world.EntityManager.SetComponentData(sunEntity, LocalTransform.Identity);

        world.EntityManager.SetComponentData(entity, new Time { Timeline = time });
        world.EntityManager.SetComponentData(entity, new Celestium
        {
            SunTransform = sunEntity,
            MoonTransform = moonEntity,
            SimulationType = preset.SimulationType,
            Latitude = preset.Latitude,
            Longitude = preset.Longitude,
            Utc = preset.Utc
        });

        var parentSystem = world.GetOrCreateSystem<ParentSystem>();
        var celestiumSystem = world.GetOrCreateSystem<CelestiumSystem>();

        parentSystem.Update(world.Unmanaged);
        celestiumSystem.Update(world.Unmanaged);

        world.EntityManager.CompleteAllTrackedJobs();

        var moonTransform = world.EntityManager.GetComponentData<LocalTransform>(moonEntity);
        var sunTransform = world.EntityManager.GetComponentData<LocalTransform>(sunEntity);
        var celestium = world.EntityManager.GetComponentData<Celestium>(entity);

        AssertQuaternionEqual(sun.Item1, sunTransform.Rotation, 1e-3f, $"expect: {sun.Item1}, actual: {sunTransform.Rotation}");
        AssertFloat3Equal(sun.Item2, celestium.SunLocalDirection, 1e-3f, $"expect: {sun.Item2}, actual: {celestium.SunLocalDirection}");

        AssertQuaternionEqual(moon.Item1, moonTransform.Rotation, 1e-3f, $"expect: {moon.Item1}, actual: {moonTransform.Rotation}");
        AssertFloat3Equal(moon.Item2, celestium.MoonLocalDirection, 1e-3f, $"expect: {moon.Item2}, actual: {celestium.MoonLocalDirection}");
    }
    
    public static void AssertQuaternionEqual(quaternion expected, quaternion actual, float delta, string message = "")
    {
        var angleDiff = math.angle(expected, actual);

        Assert.LessOrEqual(angleDiff, delta, message);
    }

    public static void AssertFloat3Equal(float3 expected, float3 actual, float delta, string message = "")
    {
        var isWithinDelta = math.abs(expected.x - actual.x) <= delta &&
                            math.abs(expected.y - actual.y) <= delta &&
                            math.abs(expected.z - actual.z) <= delta;

        Assert.IsTrue(isWithinDelta, message);
    }
}