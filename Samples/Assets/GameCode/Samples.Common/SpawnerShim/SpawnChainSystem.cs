using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Samples.Common;
using Unity.Transforms;

namespace Samples.Common
{
    // The SpawnChainSystem creates a number of chains of entities based on
    // an pre-existing group of entities that each have a SpawnChain component.
    public class SpawnChainSystem : ComponentSystem
    {
        struct SpawnChainInstance
        {
            public int spawnerIndex;
            public Entity sourceEntity;
            public float3 position;
        }

        ComponentGroup m_MainGroup;

        protected override void OnCreateManager(int capacity)
        {
            m_MainGroup  = GetComponentGroup(typeof(SpawnChain),typeof(Position));
        }

        protected override void OnUpdate()
        {
            var uniqueTypes = new List<SpawnChain>(10);
            EntityManager.GetAllUniqueSharedComponentDatas(uniqueTypes);

            // The example chain scene has a single GameObject with a SpawnRandomCircleComponent.
            // This component has a field for a prefab of the Entity you want
            // to spawn in a circle. The SpawnRandomCircleSystem uses the EntityManager to spawn 
            // a number of Entities in a circle based on the given prefab. The EntityManager
            // uses the Components attached to the prefab to create the archetype for the entities
            // it instantiates. See SpawnRandomCircleSystem.cs for more details.

            // The code below looks for all the entities in the World that have a SpawnChain Component
            // and sorts them into groups that share the same value of SpawnChain.
            // For each group of those, the number of entities in that ComponentGroup is counted.
            // If you were to attach a SpawnChain component to 10 different entities, and give each component
            // some different value (for example, a different number of links in the chain), then each
            // Entity would be filtered into its own group of a single Entity here. In the example scene
            // there is just one unique value for SpawnChain, and 200 entities share it.
            int spawnInstanceCount = 0;
            for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
            {
                var spawner = uniqueTypes[sharedIndex];
                // only select entities with a SpawnChain value that equals the one we've got
                m_MainGroup.SetFilter(spawner);
                var entities = m_MainGroup.GetEntityArray();
                // count the number of entities in this group and add it to the total
                spawnInstanceCount += entities.Length;
            }

            if (spawnInstanceCount == 0)
                return;

            // For each unique SpawnChain value, there may be several entities (in this case, since there were 200
            // spawned from the same prefab by the preceding SpawnCircleComponent, there is one group of 200 entities.
            // This code goes over each group of these entities and creates a SpawnChainInstance that describes
            // which entity is the source (top of the chain) and where it is. These are stored in a temporary
            // array because they are only used for the bit of code that comes after this.
            var spawnInstances = new NativeArray<SpawnChainInstance>(spawnInstanceCount, Allocator.Temp);
            {
                int spawnIndex = 0;
                for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
                {
                    var spawner = uniqueTypes[sharedIndex];
                    m_MainGroup.SetFilter(spawner);
                    var entities = m_MainGroup.GetEntityArray();
                    var positions = m_MainGroup.GetComponentDataArray<Position>();

                    for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                    {
                        var spawnInstance = new SpawnChainInstance();

                        spawnInstance.sourceEntity = entities[entityIndex];
                        spawnInstance.spawnerIndex = sharedIndex;
                        spawnInstance.position = positions[entityIndex].Value;

                        spawnInstances[spawnIndex] = spawnInstance;
                        spawnIndex++;
                    }
                }
            }

            // Now for the code that creates the actual chains. This code runs for each SpawnChainInstance
            // created above, and adds 100 new entities that are linked to one another. If you step
            // through the code you will find that the first entity created in this loop has an index
            // of 201, which checks out with what we did above (creation of 200 entities as source
            // entities for each chain).
            // After creating the 100 entities, which are stored in a NativeArray called "entities",
            // the code adds some components to each - a position constraint and a position. The
            // SpawnChain component is then removed from the source entity so that another chain isn't
            // created on the next update (If you look above, you can see that the code that counts up
            // the SpawnChain components exits the OnUpdate if there are no SpawnChains found).
            // The entities variable is then disposed of. Any NativeArray instances allocated 
            // in C# must be disposed of manually, unlike managed containers like System.Collections.Generic.List.
            for (int spawnIndex = 0; spawnIndex < spawnInstances.Length; spawnIndex++)
            {
                int spawnerIndex = spawnInstances[spawnIndex].spawnerIndex;
                var spawner = uniqueTypes[spawnerIndex];
                int count = spawner.count;
                var entities = new NativeArray<Entity>(count,Allocator.Temp);
                var prefab = spawner.prefab;
                float minDistance = spawner.minDistance;
                float maxDistance = spawner.maxDistance;
                float3 center = spawnInstances[spawnIndex].position;
                var sourceEntity = spawnInstances[spawnIndex].sourceEntity;

                EntityManager.Instantiate(prefab, entities);

                // The following code adds physical constraints for the chain links,
                // preventing them from moving too far from their parent chain links.

                // This code creates the constraint for the first link in the chain.
                // Since this link can't reference any links created before it, it
                // uses the source entity as a parent.
                {
                    PositionConstraint constraint = new PositionConstraint();

                    constraint.parentEntity = sourceEntity;
                    constraint.maxDistance = maxDistance;

                    EntityManager.AddComponentData(entities[0],constraint);
                }

                // The rest of the chain links can just use the preceding chain link as a parent.
                for (int i = 1; i < count; i++ )
                {
                    PositionConstraint constraint = new PositionConstraint();

                    constraint.parentEntity = entities[i - 1];
                    constraint.maxDistance = maxDistance;

                    EntityManager.AddComponentData(entities[i],constraint);
                }

                // The positions of each link should also be initialised.
                // The minimum distance is used so that when the program is run, the chains
                // fall down and bounce around. This visually demonstrates the action of the
                // PositionConstraintSystem acting on the PositionConstraint components.
                float3 dv = new float3( 0.0f, minDistance, 0.0f );
                for (int i = 0; i < count; i++)
                {
                    var position = new Position
                    {
                        Value = center - (dv * (float) i)
                    };
                    EntityManager.SetComponentData(entities[i],position);
                }

                EntityManager.RemoveComponent<SpawnChain>(sourceEntity);

                entities.Dispose();
            }

            // Now that the SpawnChainInstance data has been used to create the chains, we can dispose of it.
            spawnInstances.Dispose();
        }
    }
}
