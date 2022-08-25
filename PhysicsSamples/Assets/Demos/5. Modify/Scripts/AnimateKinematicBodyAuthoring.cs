using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

struct TeleportKinematicBody : IComponentData {}

struct AnimateKinematicBodyCurve : ISharedComponentData, IEquatable<AnimateKinematicBodyCurve>
{
    public AnimationCurve TranslationCurve;
    public AnimationCurve OrientationCurve;

    public bool Equals(AnimateKinematicBodyCurve other) =>
        Equals(TranslationCurve, other.TranslationCurve) && Equals(OrientationCurve, other.OrientationCurve);

    public override bool Equals(object obj) => obj is AnimateKinematicBodyCurve other && Equals(other);

    public override int GetHashCode() =>
        unchecked((int)math.hash(new int2(TranslationCurve?.GetHashCode() ?? 0, OrientationCurve?.GetHashCode() ?? 0)));
}

// translate a body along the z-axis and rotate about the y-axis following animation curves
[RequireComponent(typeof(PhysicsBodyAuthoring))]
class AnimateKinematicBodyAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public enum Mode
    {
        Simulate,
        Teleport
    }

    #pragma warning disable 649
    public Mode AnimateMode;
    #pragma warning restore 649

    // default translates 6 units backward in 1 second at a constant velocity and repeats from the start
    public AnimationCurve TranslationCurve = new AnimationCurve(
        new Keyframe(0f, 3f, -6f, -6f),
        new Keyframe(1f, -3f, -6f, -6f)
    )
    {
        preWrapMode = WrapMode.Loop,
        postWrapMode = WrapMode.Loop
    };

    // default repeatedly rotates smoothly between negative and positive 15 degrees about the y-axis over 2 seconds
    public AnimationCurve OrientationCurve = new AnimationCurve(
        new Keyframe(0f, -15f, 0f, 0f),
        new Keyframe(1f, 15f, 0f, 0f),
        new Keyframe(2f, -15f, 0f, 0f)
    )
    {
        preWrapMode = WrapMode.Loop,
        postWrapMode = WrapMode.Loop
    };

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (AnimateMode == Mode.Teleport)
            dstManager.AddComponent<TeleportKinematicBody>(entity);

        dstManager.AddSharedComponentData(entity, new AnimateKinematicBodyCurve
        {
            TranslationCurve = TranslationCurve,
            OrientationCurve = OrientationCurve
        });
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
partial class AnimateKinematicBodySystem : SystemBase
{
    // system state component used to identify new animated bodies on their first frame
    struct Initialized : ISystemStateComponentData {}

    // sample the animation curves to generate a new position and orientation
    // curves translate along the z-axis and set an orientation rotated about the y-axis
    static void Sample(in AnimateKinematicBodyCurve curve, in float t, ref float3 position, ref quaternion orientation)
    {
        position.z = curve.TranslationCurve.Evaluate(t);
        orientation = quaternion.AxisAngle(math.up(), math.radians(curve.OrientationCurve.Evaluate(t)));
    }

    protected override void OnUpdate()
    {
        float elapsedTime = (float)Time.ElapsedTime;

        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        // teleport all new bodies into place on the first frame
        // otherwise they may collide with things between their initial location and the first frame of animation
        Entities
            .WithName("InitializeNewAnimatedKinematicBodiesJob")
            .WithoutBurst()
            .WithNone<Initialized>()
            .ForEach(
                (
                    Entity entity,
                    ref Translation translation, ref Rotation rotation, in AnimateKinematicBodyCurve curve
                ) =>
                {
                    // sample curves and apply results directly to Translation and Rotation
                    Sample(curve, elapsedTime, ref translation.Value, ref rotation.Value);
                    commandBuffer.AddComponent<Initialized>(entity);
                }
            ).Run();

        // moving kinematic bodies via their Translation and Rotation components will teleport them to the target location
        Entities
            .WithName("TeleportAnimatedKinematicBodiesJob")
            .WithoutBurst()
            .WithAll<TeleportKinematicBody, Initialized>()
            .ForEach(
                (
                    ref Translation translation, ref Rotation rotation,
                    in PhysicsMass mass, in AnimateKinematicBodyCurve curve
                ) =>
                    // sample curves and apply results directly to Translation and Rotation
                    Sample(curve, elapsedTime, ref translation.Value, ref rotation.Value)
            ).Run();

        var tickSpeed = 1f / Time.DeltaTime;

        // moving kinematic bodies via their PhysicsVelocity component will generate contact events with any bodies they pass through
        // use PhysicsVelocity.CalculateVelocityToTarget() to compute the velocity required to move to a desired target position
        // NOTE: if you want to avoid incorrect contact events, you can teleport kinematic bodies only on frames when there is discontinuity in their motion
        // if you do so, make sure you also set PhysicsGraphicalSmoothing.ApplySmoothing = 0 on that frame if using it, to prevent incorrect interpolation
        Entities
            .WithName("SimulateAnimatedKinematicBodiesJob")
            .WithoutBurst()
            .WithAll<Initialized>()
            .WithNone<TeleportKinematicBody>()
            .ForEach(
                (
                    ref PhysicsVelocity velocity,
                    in Translation translation, in Rotation rotation, in PhysicsMass mass, in AnimateKinematicBodyCurve curve
                ) =>
                {
                    // sample curves to determine target position and orientation
                    var targetTransform = new RigidTransform(rotation.Value, translation.Value);
                    Sample(curve, elapsedTime, ref targetTransform.pos, ref targetTransform.rot);

                    // modify PhysicsVelocity to move to the target location
                    velocity = PhysicsVelocity.CalculateVelocityToTarget(mass, translation, rotation, targetTransform, tickSpeed);
                }
            ).Run();

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose(); // Can't use using above as getting DCICE002 error
    }
}
