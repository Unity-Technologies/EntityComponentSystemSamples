using Unity.Entities;
using Unity.Physics;

namespace Unity.NetCode
{
    /// <summary>
    /// Component used to control the predicted physics loop. Let you to configure each frame how the
    /// physics world is built ("incrementally" or full) and if physics should run using immediate mode
    /// (that can be quite better for small scale physics simulation).
    /// </summary>
    internal struct BuildPhysicsWorldSettings : IComponentData
    {
        /// <summary>
        /// Build the physics world using immediate mode. All work is done synchronoulsy on the main thread.
        /// </summary>
        public byte UseImmediateMode;
        /// <summary>
        /// Step the physics world using immediate mode. All work is done synchronoulsy on the main thread.
        /// </summary>
        public byte StepImmediateMode;
        /// <summary>
        /// Don't rebuild the physics world data, instead update the pre-existing rigid bodies, motions with the
        /// current transforms, physics velocities and properties.
        /// The broadphase tree is not rebuilt from scratch but updated using the current physics velocity and gravity (see
        /// <see cref="CollisionWorld.UpdateDynamicTree"/>).
        /// </summary>
        public byte UpdateBroadphaseAndMotion;
        /// <summary>
        /// The number of physics update since the first prediction tick.
        /// </summary>
        public int CurrentPhysicsStep;
    }
}
