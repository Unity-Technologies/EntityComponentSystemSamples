using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics
{
    public class TreeSpawnerAuthoring : MonoBehaviour
    {
        public GameObject TreePrefab;
        public UnityEngine.Material DeadTreeMaterial;
        public float TreeDensity = 1.0f;
        public float TreeGrowProbability = 0.25f;
        public float MaxGrowTime = 15;
        public float MaxDeadTime = 15;
        public float ReGrowDelay = 5;
        public float GroundSize = 15;
        public bool EnableColourChange = false;

        class TreeSpawnerBaker : Baker<TreeSpawnerAuthoring>
        {
            public override void Bake(TreeSpawnerAuthoring authoring)
            {
                DependsOn(authoring.TreePrefab);
                if (authoring.TreePrefab == null) return;
                var prefabTreeEntity = GetEntity(authoring.TreePrefab, TransformUsageFlags.Dynamic);

                // Is added to the prefab
                var createComponent = new TreeSpawnerComponent
                {
                    TreeEntity = prefabTreeEntity,
                    DeadTreeMaterial = new UnityObjectRef<UnityEngine.Material> { Value = authoring.DeadTreeMaterial },
                    MaxGrowTime = authoring.MaxGrowTime,
                    MaxDeadTime = authoring.MaxDeadTime,
                    ReGrowDelay = authoring.ReGrowDelay,
                    TreeDensity = authoring.TreeDensity,
                    TreeGrowProbability = authoring.TreeGrowProbability,
                    GroundSize = authoring.GroundSize,
                    EnableColourChange = authoring.EnableColourChange
                };
                var treeSpawnerEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(treeSpawnerEntity, createComponent);
            }
        }
    }

    // This component is used once to initialize the tree spawner and then deleted
    public struct TreeSpawnerComponent : IComponentData
    {
        public Entity TreeEntity;
        public UnityObjectRef<UnityEngine.Material> DeadTreeMaterial;
        public int DeadTreeMaterialIndex;
        public float TreeDensity;
        public float TreeGrowProbability;
        public float MaxGrowTime;
        public float MaxDeadTime;
        public float ReGrowDelay;
        public float GroundSize;
        public bool EnableColourChange;
    }

    // A component placed on the tree prefab entity (aka: tree root) to keep track of the life cycle of the tree
    public struct TreeComponent : IComponentData
    {
        public float3 SpawningPosition;
        public float GrowTime;
        public float DeadTime;

        public int GrowTimer;
        public int DeathTimer;
        public int RegrowTimer;
        public LifeCycleStates LifeCycleTracker;
    }

    // Used to track the life cycle of the tree rather than adding tags and doing structural changes
    // For states with names beginning with 'Is_': these states all decrement various timers.
    // For states with names beginning with 'TransitionTo_': these states are used as flags to signal external systems
    public enum LifeCycleStates
    {
        IsGrowing,                  // countdown state to decrement GrowTimer
        TransitionToDead,           // Flag for TreeDeathSystem: Turn tree orange, transition trunk & top from static to dynamic bodies
        IsDead,                     // countdown state to decrement DeathTimer
        TransitionToDelete,         // Flag for TreeDeletionSystem: delete the tree top and tree trunk entities
        IsRegrown,                  // countdown state to decrement RegrowTimer
        TransitionToInsert          // Flag for TreeRegrowSystem: respawn the tree
    }

    // Track the state added to the each piece of the tree (root, top, trunk) to identify it for the systems outside of TreeLifetimeSystem
    public struct TreeState : IComponentData
    {
        public enum States : byte
        {
            Default,                    // Carry on
            TriggerTreeGrowthSystem,    // Set: TreeGrowthSystem, Used: TreeGrowthSystem, Lifecycle: IsGrowing
            TriggerWholeTreeToDynamic,  // Set: TreeLifecycleSystem, Used: TreeDeathSystem, Lifecycle: TransitionToDead
            TriggerChangeTreeColor,     // Set: TreeLifecycleSystem during TransitionToDead, Used: TreeDeathSystem, Lifecycle: TransitionToDead
            TransitionToDeadDone,       // Set: TreeDeathSystem, Used: TreeDeathSystem, Lifecycle: TransitionToDead
            TriggerDeleteTrunkAndTop,   //Set: TreeLifecycleSystem, Used: TreeDeletionSystem, Lifecycle: TransitionToDelete
        }

        public static TreeState Default => new TreeState { Value = States.Default };

        public States Value;
    }

    // Tag used in TreeRegrowSystem to identify what entities need a second pass to be respawned
    public struct TempIntermediateTreeSpawningTag : IComponentData {}
}
