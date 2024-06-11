using Unity.Entities;
using Unity.Physics.Systems;

namespace Unity.Physics.Tests
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class ServerPhysicsGroup : CustomPhysicsSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<ServerPhysicsSingleton>();
        }

#if !HAVOK_PHYSICS_EXISTS

        public ServerPhysicsGroup() : base(1, true) {}

#else
        internal static Havok.Physics.HavokConfiguration GetHavokConfiguration()
        {
            var havokConfiguration = Havok.Physics.HavokConfiguration.Default;
            havokConfiguration.VisualDebugger.Enable = 1;
            havokConfiguration.VisualDebugger.Port += 1;
            return havokConfiguration;
        }

        public ServerPhysicsGroup() : base(1, true, GetHavokConfiguration()) {}

#endif
    }
}
