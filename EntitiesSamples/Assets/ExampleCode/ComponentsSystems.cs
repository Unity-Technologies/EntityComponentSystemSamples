using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#if false
namespace ExampleCode.Components
{
    public struct EnergyShield : IComponentData
    {
        public int HitPoints;
        public int MaxHitPoints;
        public float RechargeDelay;
        public float RechargeRate;
    }

    public struct OnFire : IComponentData
    {
        // An empty component is called a "tag component".
        // Tag components take no storage space, but they can be
        // queried, added, and removed like any other component.
    }
}

namespace ExampleCode.SystemsAndSystemGroups
{
    // An example system that creates and destroys entities.
    // This system will be added to the system group called MySystemGroup.
    // The ISystem methods are made Burst 'entry points' by marking
    // them with the BurstCompile attribute.
    [UpdateInGroup(typeof(MySystemGroup))]
    public partial struct MySystem : ISystem
    {
        // Called once when the system is created.
        // Can be omitted when empty.
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        // Called once when the system is destroyed.
        // Can be omitted when empty.
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        // Usually called every frame. When exactly a system is updated
        // is determined by the system group to which it belongs.
        // Can be omitted when empty.
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }

    // An example system group.
    public partial class MySystemGroup : ComponentSystemGroup
    {
        // A system group is left empty unless you want
        // to override OnUpdate, OnCreate, or OnDestroy.
    }
}

namespace ExampleCode.Queries
{
    public partial struct MySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityQuery myQuery = SystemAPI.QueryBuilder().WithAll<Foo, Bar, Apple>().WithNone<Banana>().Build();
            ComponentTypeHandle<Foo> fooHandle = SystemAPI.GetComponentTypeHandle<Foo>();
            ComponentTypeHandle<Bar> barHandle = SystemAPI.GetComponentTypeHandle<Bar>();
            EntityTypeHandle entityHandle = SystemAPI.GetEntityTypeHandle();

            // Getting array copies of component values and entity ID's:
            {
                // Remember that Temp allocations do not need to be manually disposed.

                // Get an array of the Apple component values of all
                // entities that match the query.
                // This array is a *copy* of the data stored in the chunks.
                NativeArray<Apple> apples = myQuery.ToComponentDataArray<Apple>(Allocator.Temp);

                // Get an array of the ID's of all entities that match the query.
                // This array is a *copy* of the data stored in the chunks.
                NativeArray<Entity> entities = myQuery.ToEntityArray(Allocator.Temp);
            }

            // Getting the chunks matching a query and accessing the chunk data:
            {
                // Get an array of all chunks matching the query.
                NativeArray<ArchetypeChunk> chunks = myQuery.ToArchetypeChunkArray(Allocator.Temp);

                // Loop over all chunks matching the query.
                for (int i = 0, chunkCount = chunks.Length; i < chunkCount; i++)
                {
                    ArchetypeChunk chunk = chunks[i];

                    // The arrays returned by `GetNativeArray` are the very same arrays
                    // stored within the chunk, so you should not attempt to dispose of them.
                    NativeArray<Foo> foos = chunk.GetNativeArray(ref fooHandle);
                    NativeArray<Bar> bars = chunk.GetNativeArray(ref barHandle);

                    // Unlike component values, entity ID's should never be
                    // modified, so the array of entity ID's is always read only.
                    NativeArray<Entity> entities = chunk.GetNativeArray(entityHandle);

                    // Loop over all entities in the chunk.
                    for (int j = 0, entityCount = chunk.Count; j < entityCount; j++)
                    {
                        // Get the entity ID and Foo component of the individual entity.
                        Entity entity = entities[j];
                        Foo foo = foos[j];
                        Bar bar = bars[j];

                        // Set the Foo value.
                        foos[j] = new Foo { };
                    }
                }
            }

            // SystemAPI.Query:
            {
                // SystemAPI.Query provides a more convenient way to loop
                // through the entities matching a query. Source generation
                // translates this foreach into the functional equivalent
                // of the prior section. Understand that SystemAPI.Query
                // should ONLY be called as the 'in' clause of a foreach.

                // Each iteration processes one entity matching a query
                // that includes Foo, Bar, Apple and excludes Banana:
                // - 'foo' is assigned a read-write reference to the Foo component
                // - 'bar' is assigned a read-only reference to the Bar component
                // - 'entity' is assigned the entity ID
                foreach (var (foo, bar, entity) in
                         SystemAPI.Query<RefRW<Foo>, RefRO<Bar>>()
                             .WithAll<Apple>()
                             .WithNone<Banana>()
                             .WithEntityAccess())
                {
                    foo.ValueRW = new Foo { };
                }
            }
        }
    }
}

namespace ExampleCode.EntityCommandBufferSystems
{
    // Define a new EntityCommandBufferSystem that will update
    // in the InitializationSystemGroup before FooSystem.
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(FooSystem))]
    public partial class MyEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        // There's not usually any reason to override the methods of `EntityCommandBufferSystem`,
        // so an empty class is the norm. In fact, needing to define your own `EntityCommandBufferSystem`
        // is uncommon in the first place because the default world already includes
        // several, such as `BeginSimulationEntityCommandBufferSystem`.
    }

    public partial struct FooSystem : ISystem
    {
    }
}

namespace ExampleCode.DynamicBuffers
{
    // Defines a DynamicBuffer<Waypoint> component type,
    // which is a growable array of Waypoint elements.
    // InternalBufferCapacity is the number of elements per
    // entity stored directly in the chunk (defaults to 8).
    [InternalBufferCapacity(20)]
    public struct Waypoint : IBufferElementData
    {
        public float3 Value;
    }

    // Example of creating and accessing a DynamicBuffer in a system.
    public partial struct MySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateEntity();

            // Adds the Waypoint component type to the entity
            // and returns the new buffer.
            DynamicBuffer<Waypoint> waypoints = state.EntityManager.AddBuffer<Waypoint>(entity);

            // Setting the length to a value greater than its current capacity resizes the buffer.
            waypoints.Length = 100;

            // Loop through the buffer to set its values.
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = new Waypoint { Value = new float3() };
            }

            // DynamicBuffers are invalidated by structural change operations
            {
                // Even though this structural change doesn't touch 'waypoints' or its entity,
                // this operation invalidates 'waypoints' and all other DynamicBuffers
                state.EntityManager.CreateEntity();

#if true
                // Because 'waypoints' has been invalidated, any read or write of
                // its content throws a safety check exception.
                var w = waypoints[0]; // exception!
#else
                // Re-acquire the DynamicBuffer instance.
                waypoints = state.EntityManager.GetBuffer<Waypoint>(entity);
                var w = waypoints[0]; // OK
#endif
            }

            // EntityCommandBuffer methods for DynamicBuffers
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

                // Records a command to remove the MyElement dynamic buffer from an entity.
                ecb.RemoveComponent<Waypoint>(entity);

                // Records a command to add a MyElement dynamic buffer to an existing entity.
                // The data of the returned DynamicBuffer is stored in the EntityCommandBuffer,
                // so changes to the returned buffer are also recorded changes.
                DynamicBuffer<Waypoint> myBuff = ecb.AddBuffer<Waypoint>(entity);

                // After playback, the entity will have a MyElement buffer with
                // Length 20 and these recorded values.
                myBuff.Length = 20;
                myBuff[0] = new Waypoint { Value = new float3() };
                myBuff[3] = new Waypoint { Value = new float3() };

                // SetBuffer is like AddBuffer, but safety checks will throw an exception at playback if
                // the entity doesn't already have a MyElement buffer.
                DynamicBuffer<Waypoint> otherBuf = ecb.SetBuffer<Waypoint>(entity);

                // Records a Waypoint value that will be appended to the buffer. Safety checks throw
                // an exception at playback if the entity doesn't already have a MyElement buffer.
                ecb.AppendToBuffer<Waypoint>(entity, new Waypoint { Value = new float3() });

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }

            // re-interpreting a DynamicBuffer
            {
                DynamicBuffer<Waypoint> myBuff = state.EntityManager.GetBuffer<Waypoint>(entity);

                // Valid because each float3 and each Waypoint struct are both 12 bytes in size.
                DynamicBuffer<float3> floatsBuffer = myBuff.Reinterpret<float3>();

                // 'floatsBuffer' and 'myBuff' represent the same content, so these two assignments have the same effect
#if true
                floatsBuffer[2] = new float3(1, 2, 3);
#else
                myBuff[2] = new Waypoint { Value = new float3(1, 2, 3) };
#endif
            }
        }
    }
}

namespace ExampleCode.EnableableComponents
{
    // An example enableable component type.
    // Structs implementing IComponentData or IBufferElementData
    // can also implement IEnableableComponent.
    // `EntityManager`, `ComponentLookup<T>`, and `ArchetypeChunk` all have methods
    //for checking and setting the enabled state of components.
    public struct Health : IComponentData, IEnableableComponent
    {
        public float Value;
    }

    // A system demonstrating use of an enableable component type.
    [BurstCompile]
    public partial struct MySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check and set the enabled state.
            {
                EntityManager em = state.EntityManager;

                // Create a single entity and add the Health component.
                Entity myEntity = em.CreateEntity();
                em.AddComponent<Health>(myEntity);

                // Components begin life enabled, so this returns true.
                bool b = em.IsComponentEnabled<Health>(myEntity);

                // Disable the Health component of myEntity
                em.SetComponentEnabled<Health>(myEntity, false);

                ComponentLookup<Health> healthLookup = SystemAPI.GetComponentLookup<Health>();

                // Though disabled, the component can still be read and modified.
                Health h = healthLookup[myEntity];

                // We can also check and set the enabled state through the ComponentLookup.
                b = healthLookup.IsComponentEnabled(myEntity);
                healthLookup.SetComponentEnabled(myEntity, false);
            }

            EntityQuery myQuery = SystemAPI.QueryBuilder().WithAll<Health>().Build();

            // Query methods.
            {
                // The disabled entities will NOT be included in the results.
                var entities = myQuery.ToEntityArray(Allocator.Temp);
                var healths = myQuery.ToComponentDataArray<Health>(Allocator.Temp);

                EntityQuery myQueryIgnoreEnabled = SystemAPI.QueryBuilder().WithAll<Health>()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState).Build();

                // The disabled entities WILL be included in the results.
                entities = myQueryIgnoreEnabled.ToEntityArray(Allocator.Temp);
                healths = myQueryIgnoreEnabled.ToComponentDataArray<Health>(Allocator.Temp);
            }

            // Check and set the enabled state of each entity in a chunk.
            {
                ComponentTypeHandle<Health> healthHandle = SystemAPI.GetComponentTypeHandle<Health>();

                var chunks = myQuery.ToArchetypeChunkArray(Allocator.Temp);
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunk = chunks[i];

                    // Loop through the entities of the chunk.
                    for (int entityIdx = 0, entityCount = chunk.Count; entityIdx < entityCount; entityIdx++)
                    {
                        // Read the enabled state of the
                        // entity's Health component.
                        bool enabled = chunk.IsComponentEnabled(ref healthHandle, entityIdx);

                        // Disable the entity's Health component.
                        chunk.SetComponentEnabled(ref healthHandle, entityIdx, false);
                    }
                }
            }
        }
    }
}

namespace ExampleCode.Aspects
{
    // An example aspect which wraps a Foo component
    // and the enabled state of a Bar component.
    public readonly partial struct MyAspect : IAspect
    {
        // The aspect includes the entity ID.
        // Because it's a readonly value type,
        // there's no danger in making the field public.
        public readonly Entity Entity;

        // The aspect includes the Foo component,
        // with read-write access.
        readonly RefRW<Foo> foo;

        // A property which gets and sets the Foo component.
        public float3 Foo
        {
            get => foo.ValueRO.Value;
            set => foo.ValueRW.Value = value;
        }

        // The aspect includes the enabled state of
        // the Bar component.
        public readonly EnabledRefRW<Bar> BarEnabled;
    }

    public struct Foo : IComponentData
    {
        public float3 Value;
    }

    public struct Bar : IComponentData, IEnableableComponent
    {
        public float Value;
    }

    /*
     * These methods return instances of an aspect:

        - `SystemAPI.GetAspectRW<T>(Entity)`
        - `SystemAPI.GetAspectRO<T>(Entity)`
        - `EntityManager.GetAspect<T>(Entity)`
        - `EntityManager.GetAspectRO<T>(Entity)`

        These methods throw if the `Entity` passed doesn't have all the components included in aspect `T`.

        Aspects returned by `GetAspectRO()` will throw if you use any method or property that attempts to modify the underlying components.

        You can also get aspect instances by including them as parameters of an `IJobEntity`'s `Execute` method or as type parameters of `SystemAPI.Query`.
     */
}
#endif
