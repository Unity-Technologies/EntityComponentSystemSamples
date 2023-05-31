using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Modify
{
// translate a body along the z-axis and rotate about the y-axis following animation curves
    [RequireComponent(typeof(PhysicsBodyAuthoring))]
    class AnimateKinematicBodyAuthoring : MonoBehaviour
    {
        public enum Mode
        {
            Simulate,
            Teleport
        }

        public Mode AnimateMode;

        // default translates 6 units backward in 1 second at a constant velocity and repeats from the start
        public AnimationCurve TranslationCurve = new AnimationCurve(
            new Keyframe(0f, 3f, -6f, -6f),
            new Keyframe(1f, -3f, -6f, -6f))
        {
            preWrapMode = WrapMode.Loop,
            postWrapMode = WrapMode.Loop
        };

        // default repeatedly rotates smoothly between negative and positive 15 degrees about the y-axis over 2 seconds
        public AnimationCurve OrientationCurve = new AnimationCurve(
            new Keyframe(0f, -15f, 0f, 0f),
            new Keyframe(1f, 15f, 0f, 0f),
            new Keyframe(2f, -15f, 0f, 0f))
        {
            preWrapMode = WrapMode.Loop,
            postWrapMode = WrapMode.Loop
        };

        class Baker : Baker<AnimateKinematicBodyAuthoring>
        {
            public override void Bake(AnimateKinematicBodyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (authoring.AnimateMode == AnimateKinematicBodyAuthoring.Mode.Teleport)
                {
                    AddComponent<TeleportKinematicBody>(entity);
                }

                AddSharedComponentManaged(entity, new AnimateKinematicBodyCurve
                {
                    TranslationCurve = authoring.TranslationCurve,
                    OrientationCurve = authoring.OrientationCurve
                });
            }
        }
    }

    struct TeleportKinematicBody : IComponentData
    {
    }

    struct AnimateKinematicBodyCurve : ISharedComponentData, IEquatable<AnimateKinematicBodyCurve>
    {
        public AnimationCurve TranslationCurve;
        public AnimationCurve OrientationCurve;

        public bool Equals(AnimateKinematicBodyCurve other) =>
            Equals(TranslationCurve, other.TranslationCurve) && Equals(OrientationCurve, other.OrientationCurve);

        public override bool Equals(object obj) => obj is AnimateKinematicBodyCurve other && Equals(other);

        public override int GetHashCode() =>
            unchecked((int)math.hash(new int2(TranslationCurve?.GetHashCode() ?? 0,
                OrientationCurve?.GetHashCode() ?? 0)));
    }
}
