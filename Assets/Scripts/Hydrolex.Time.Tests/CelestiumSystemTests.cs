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
            yield return new TestCaseData(
                12.0f,
                new Celestium { SimulationType = CelestiumSimulation.Simple, Latitude = 0, Longitude = 0, Utc = 0 },
                new quaternion(0.0f, 0.707f, -0.707f, 0.0f),
                new quaternion(-0.707f, 0.0f, 0.0f, 0.707f),
                math.down(),
                math.up()
            ).SetName("SimpleCenterPositionMidDay");
        }
    }

    [Test, TestCaseSource(nameof(CelestiumTestCases))]
    public void GetSunDirectionTest(float time, Celestium preset, quaternion sunRotation, quaternion moonRotation, float3 sunDirection, float3 moonDirection)
    {
        if (preset.SimulationType == CelestiumSimulation.Simple)
        {
            var actual = CelestiumSystem.GetChimericalSunDirection(preset, time);

            AssertQuaternionEqual(sunRotation, actual, 1E-3f, $"expect: {sunRotation}, actual: {actual}");
        }
    }

    [Test, TestCaseSource(nameof(CelestiumTestCases))]
    public void GetMoonDirectionTest(float time, Celestium preset, quaternion sunRotation, quaternion moonRotation, float3 sunDirection, float3 moonDirection)
    {
        if (preset.SimulationType == CelestiumSimulation.Simple)
        {
            var actual = CelestiumSystem.GetChimericalMoonDirection(sunDirection);

            AssertQuaternionEqual(moonRotation, actual, 1E-3f, $"expect: {moonRotation}, actual: {actual}");
        }
    }

    [Test, TestCaseSource(nameof(CelestiumTestCases))]
    public void UpdateTest(float time, Celestium preset, quaternion sunRotation, quaternion moonRotation, float3 sunDirection, float3 moonDirection)
    {
        using var world = new World("Test world");

        var entity = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(Celestium), typeof(Time));
        var moon = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(Parent));
        var sun = world.EntityManager.CreateEntity(typeof(LocalTransform), typeof(Parent));

        world.EntityManager.SetComponentData(moon, new Parent { Value = entity });
        world.EntityManager.SetComponentData(sun, new Parent { Value = entity });
        world.EntityManager.SetComponentData(entity, LocalTransform.Identity);
        world.EntityManager.SetComponentData(moon, LocalTransform.Identity);
        world.EntityManager.SetComponentData(sun, LocalTransform.Identity);

        world.EntityManager.SetComponentData(entity, new Time { Timeline = time });
        world.EntityManager.SetComponentData(entity, new Celestium
        {
            SunTransform = sun,
            MoonTransform = moon,
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

        var moonTransform = world.EntityManager.GetComponentData<LocalTransform>(moon);
        var sunTransform = world.EntityManager.GetComponentData<LocalTransform>(sun);
        var celestium = world.EntityManager.GetComponentData<Celestium>(entity);

        AssertQuaternionEqual(sunRotation, sunTransform.Rotation, 1E-3f, $"expect: {sunRotation}, actual: {sunTransform.Rotation}");
        AssertFloat3Equal(sunDirection, celestium.SunLocalDirection, 1E-3f, $"expect: {sunDirection}, actual: {celestium.SunLocalDirection}");

        AssertQuaternionEqual(moonRotation, moonTransform.Rotation, 1E-3f, $"expect: {moonRotation}, actual: {moonTransform.Rotation}");
        AssertFloat3Equal(moonDirection, celestium.MoonLocalDirection, 1E-3f, $"expect: {moonDirection}, actual: {celestium.MoonLocalDirection}");
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