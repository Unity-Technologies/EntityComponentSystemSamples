using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using System;

namespace Unity.NetCode
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
        /// <summary>
        /// When enable, the physics world (and in particular the broadphase tree) is build from scratch only for the first predicted tick
        /// (when the prediction start). For all the sub-sequent predicted ticks, the broadphase AABB tree is only
        /// updated using the calculated physics velocity from the previous physics step and the gravity. This lead to better performance
        /// than rebuilding from scratch, at the cost of slighlty worse broadphase culling.
        /// The following conditions must be respected to avoid a full physics world build:
        /// - No dynamic physics object has been created or destroyed inside the prediction loop
        /// </summary>
        public byte BuildPhysicsWorldOnceThenUpdate;
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
