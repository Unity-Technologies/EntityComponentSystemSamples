using Unity.Entities;
using Unity.Transforms;

namespace Samples.HelloNetcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [UpdateAfter(typeof(BarrelSpawnerSystem))]
    public partial class StaticProblemSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnableOptimization>();
        }

        protected override void OnUpdate()
        {
            var setup = SystemAPI.GetSingleton<BarrelSetup>();
            if (!setup.EnableStaticOptimizationProblem)
            {
                Enabled = false;
            }


            /* This is intentionally incorrect. We grab write access to the transform data but never modify it. */
            foreach (var trans in SystemAPI.Query<RefRW<LocalTransform>>())
            {

            }
        }
    }
}
