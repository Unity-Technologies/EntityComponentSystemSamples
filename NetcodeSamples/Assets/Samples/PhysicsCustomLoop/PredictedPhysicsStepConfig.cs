using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using System;

namespace Unity.Samples.Netcode
{
    /// <summary>
    /// Component used to configure how physics rebuild the and step the predicted physic world.
    /// </summary>
    [Serializable]
    public struct PhysicsLoopConfig : IComponentData
    {
        /// <summary>
        /// Use immediate mode (no jobs, only mainthread) to step the physics world. This can be faster than
        /// using jobs in case the number of entities is relatively small.
        /// </summary>
        public byte StepImmediateMode;
        /// <summary>
        /// Use immediate mode (no jobs, only mainthread) to build or update the physics world. This can be faster than
        /// using jobs in case the number of entities is relatively small.
        /// </summary>
        public byte UseImmediateMode;
    }

    /// <summary>
    /// Auhoring behaviour that can be used to configure the predicted physics loop update. It will bake
    /// bake a <see cref="PhysicsLoopConfig"/> component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PredictedPhysicsStepConfig : MonoBehaviour
    {
        public PhysicsLoopConfig Config;

        class Baker : Baker<PredictedPhysicsStepConfig>
        {
            public override void Bake(PredictedPhysicsStepConfig authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.None), authoring.Config);
            }
        }
    }
}
