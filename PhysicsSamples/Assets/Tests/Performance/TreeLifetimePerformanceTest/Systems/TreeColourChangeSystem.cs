// This system changes the colour of the tree top from green to orange when DeadTree.EnableColourChange is true.
// This system iterates on entities based on the TreeFlag value where the flag value = TriggerChangeTreeColor (this is
// set in TreeDeathSystem). It is a non-issue if this system is not run, and the flag value is not updated. After this
// state change, the flag values in the TreeTop and TreeTrunk aren't used for anything before the entity is destroyed.
// The colour change will happen during the LifeCycleStates.IsDead state.
// Goals: a purely visual tool to show when a tree is killed and is dynamic
using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Rendering;

namespace Unity.Physics
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [UpdateAfter(typeof(TreeDeathSystem))]
    public partial struct TreeColourChangeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeSpawnerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spawner = SystemAPI.GetSingleton<TreeSpawnerComponent>();
            if (!spawner.EnableColourChange)
            {
                return;
            }

            // Change the color of the tree tops:
            state.Dependency = new ChangeTreeColourJob
            {
                DeadTreeMaterialIndex = spawner.DeadTreeMaterialIndex
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        [WithAll(typeof(TreeState), typeof(PhysicsCollider), typeof(TreeTopTag))]
        partial struct ChangeTreeColourJob : IJobEntity
        {
            public int DeadTreeMaterialIndex;

            void Execute(ref TreeState treeState, ref MaterialMeshInfo materialMeshInfo)
            {
                if (treeState.Value == TreeState.States.TriggerChangeTreeColor)
                {
                    materialMeshInfo.Material = DeadTreeMaterialIndex;
                    treeState.Value = TreeState.States.TransitionToDeadDone;
                }
            }
        }
    }
}
