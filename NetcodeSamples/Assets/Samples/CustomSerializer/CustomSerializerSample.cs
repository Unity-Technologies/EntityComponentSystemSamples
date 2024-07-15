using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Random = UnityEngine.Random;

namespace Samples.CustomChunkSerializer
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CustomSerializerSample : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GhostCollection>();
            RequireForUpdate<TestCustomSerializer>();
        }

        public static Entity CreateEntityArchetype(EntityManager entityManager, bool initValues)
        {
            var entity = entityManager.CreateEntity();
            var child0 = entityManager.CreateEntity(typeof(GhostChildEntity));
            var child1 = entityManager.CreateEntity(typeof(GhostChildEntity));
            var linkedEntityGroups = entityManager.AddBuffer<LinkedEntityGroup>(entity);
            linkedEntityGroups.Add(entity);
            linkedEntityGroups.Add(child0);
            linkedEntityGroups.Add(child1);
            entityManager.AddComponent(entity, ComponentType.ReadWrite<GhostOwner>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<LocalTransform>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<IntCompo1>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<IntCompo2>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<IntCompo3>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<FloatCompo1>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<FloatCompo2>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<FloatCompo3>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<InterpolatedOnlyComp>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<OwnerOnlyComp>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<Buf1>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<Buf2>());
            entityManager.AddComponent(entity, ComponentType.ReadWrite<Buf3>());
            entityManager.AddComponent(child0, ComponentType.ReadWrite<IntCompo1>());
            entityManager.AddComponent(child0, ComponentType.ReadWrite<FloatCompo1>());
            entityManager.AddComponent(child0, ComponentType.ReadWrite<Buf1>());
            entityManager.AddComponent(child1, ComponentType.ReadWrite<IntCompo2>());
            entityManager.AddComponent(child1, ComponentType.ReadWrite<FloatCompo2>());
            entityManager.AddComponent(child1, ComponentType.ReadWrite<Buf2>());

            if (initValues)
            {
                entityManager.SetComponentData(entity, new IntCompo1 { Value = 100 });
                entityManager.SetComponentData(entity, new IntCompo2 { Value = 200 });
                entityManager.SetComponentData(entity, new IntCompo3 { Value = 300 });

                entityManager.SetComponentData(entity, new FloatCompo1 { Value = 10f });
                entityManager.SetComponentData(entity, new FloatCompo2 { Value = 20f });
                entityManager.SetComponentData(entity, new FloatCompo3 { Value = 30f });
				entityManager.SetComponentData(entity, new InterpolatedOnlyComp{Value1 = 10, Value2 = 20, Value3 = 30, Value4 = 40});
                entityManager.SetComponentData(entity, new OwnerOnlyComp{Value1 = 10, Value2 = 20, Value3 = 30, Value4 = 40});
                entityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3(1f, 2f, 3f)));

                var buf1 = entityManager.GetBuffer<Buf1>(entity);
                buf1.ResizeUninitialized(5);
                for (int i = 0; i < buf1.Length; ++i)
                    buf1.ElementAt(i).Value = 10;
                var buf2 = entityManager.GetBuffer<Buf2>(entity);
                buf2.ResizeUninitialized(5);
                for (int i = 0; i < buf2.Length; ++i)
                    buf2.ElementAt(i).Value = 20;
                var buf3 = entityManager.GetBuffer<Buf3>(entity);
                buf3.ResizeUninitialized(5);
                for (int i = 0; i < buf3.Length; ++i)
                {
                    buf3.ElementAt(i).Value1 = 10;
                    buf3.ElementAt(i).Value2 = 20;
                    buf3.ElementAt(i).Value3 = 30f;
                    buf3.ElementAt(i).Value4 = 40f;
                }

                entityManager.SetComponentData(child0, new IntCompo1 { Value = 100 });
                entityManager.SetComponentData(child0, new FloatCompo1 { Value = 100f });
                buf1 = entityManager.GetBuffer<Buf1>(child0);
                buf1.ResizeUninitialized(5);
                for (int i = 0; i < buf1.Length; ++i)
                    buf1.ElementAt(i).Value = 10;

                entityManager.SetComponentData(child1, new IntCompo2 { Value = 200 });
                entityManager.SetComponentData(child1, new FloatCompo2 { Value = 200f });
                buf2 = entityManager.GetBuffer<Buf2>(child1);
                buf2.ResizeUninitialized(5);
                for (int i = 0; i < buf1.Length; ++i)
                    buf2.ElementAt(i).Value = 20;
            }

            return entity;
        }

        protected override void OnUpdate()
        {
            //First frame: create all the ghost prefabs. This is going to create
            Entity prefab = CreateEntityArchetype(EntityManager, World.IsServer());
            var comp = SystemAPI.GetSingleton<TestCustomSerializer>();
            var prefabConfig = new GhostPrefabCreation.Config
            {
                Name = "SimpleGhost",
                Importance = 1,
                SupportedGhostModes = GhostModeMask.All,
                DefaultGhostMode = GhostMode.OwnerPredicted,
                OptimizationMode = GhostOptimizationMode.Dynamic,
                UsePreSerialization = comp.usePreserialization,
                CollectComponentFunc = ChunkSerializer.CollectComponentFunc
            };
            EntityManager.SetName(prefab, prefabConfig.Name);
            GhostPrefabCreation.ConvertToGhostPrefab(EntityManager, prefab, prefabConfig);
            var hash = (Unity.Entities.Hash128)EntityManager.GetComponentData<GhostType>(prefab);
            var customSerializer = SystemAPI.GetSingletonRW<GhostCollectionCustomSerializers>();
            customSerializer.ValueRW.Serializers.Add(hash, new GhostPrefabCustomSerializer
            {
                SerializeChunk = ChunkSerializer.SerializerFunc,
                PreSerializeChunk = ChunkSerializer.PreSerializerFunc
            });
            if (World.IsServer())
            {
                SystemAPI.GetSingletonRW<GhostSendSystemData>().ValueRW.UseCustomSerializer = comp.useCustomSerializer ? 1 : 0;
				var entities = EntityManager.Instantiate(prefab, comp.numInstances, Allocator.Temp);
                for (int i = 0; i < entities.Length; ++i)
                {
                    EntityManager.SetComponentData(entities[i], new GhostOwner{NetworkId = 1});
                }
            }
            Enabled = false;
        }
    }

    [RequireMatchingQueriesForUpdate]
    public partial class AutoGoInGame : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GhostCollection>();
            RequireForUpdate<TestCustomSerializer>();
        }
        protected override void OnUpdate()
        {
            var buffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (con,ent) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
            {
                buffer.AddComponent(ent, new NetworkStreamInGame());
            }
            buffer.Playback(EntityManager);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [CreateAfter(typeof(GhostSendSystem))]
    public partial class RandomizeSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TestCustomSerializer>();
        }

        protected override void OnUpdate()
        {
            //change all the components is a bad case scenario, because that may not let the serialization
            //code to perform all the calculation (many call, zero size to write), lots of write etc
            //it is possible to maximize the amount of work by forcing the ghost send system to always send
            //up to x chunks to have some more reliable "results"
            var testCustomSerializer = SystemAPI.GetSingleton<TestCustomSerializer>();
            var ghostsQuery = GetEntityQuery(typeof(GhostInstance));
            var ghostsChunks = ghostsQuery.ToArchetypeChunkArray(Allocator.Temp);
            var entityTypeHandle = GetEntityTypeHandle();
            foreach(var c in ghostsChunks)
            {
                if (UnityEngine.Random.value > testCustomSerializer.percentChunkChange)
                    continue;
                var entities = c.GetNativeArray(entityTypeHandle);
                for (var ent = 0; ent < entities.Length; ++ent)
                {
                    if (UnityEngine.Random.value > testCustomSerializer.percentEntityChanges)
                        continue;

                    var entity = entities[ent];
                    var lg = EntityManager.GetBuffer<LinkedEntityGroup>(entity);

                    World.EntityManager.SetComponentData(entity, new IntCompo1{Value = Random.Range(-10, 20)});
                    World.EntityManager.SetComponentData(entity, new IntCompo2{Value = Random.Range(-10, 20)});
                    World.EntityManager.SetComponentData(entity, new IntCompo3{Value = Random.Range(-10, 20)});
                    World.EntityManager.SetComponentData(entity, new FloatCompo1{Value = Random.Range(-10f, 20f)});
                    World.EntityManager.SetComponentData(entity, new FloatCompo2{Value = Random.Range(-10f, 20f)});
                    World.EntityManager.SetComponentData(entity, new FloatCompo3{Value = Random.Range(-10f, 20f)});
                    {
                        var b1 = EntityManager.GetBuffer<Buf1>(lg[0].Value);
                        for (int el = 0; el < 5; ++el)
                            b1.ElementAt(el).Value = Random.Range(-10, 20);
                    }
                    {
                        var b1 = EntityManager.GetBuffer<Buf2>(lg[0].Value);
                        for (int el = 0; el < 5; ++el)
                            b1.ElementAt(el).Value = Random.Range(-10, 20);
                    }
                    {
                        var b1 = EntityManager.GetBuffer<Buf3>(lg[0].Value);
                        for (int el = 0; el < 5; ++el)
                        {
                            b1.ElementAt(el).Value1 = Random.Range(-10, 20);
                            b1.ElementAt(el).Value2 = Random.Range(-10, 20);
                            b1.ElementAt(el).Value3 = Random.Range(-10, 20);
                            b1.ElementAt(el).Value4 = Random.Range(-10, 20);
                        }

                    }
                    var child0 = lg[1].Value;
                    World.EntityManager.SetComponentData(child0, new IntCompo1{Value = Random.Range(-10, 20)});
                    World.EntityManager.SetComponentData(child0, new FloatCompo1{Value = Random.Range(-10f, 20f)});
                    {
                        var b1 = EntityManager.GetBuffer<Buf1>(lg[1].Value);
                        for (int el = 0; el < 5; ++el)
                            b1.ElementAt(el).Value = Random.Range(-10, 20);
                    }
                    var child1 = lg[2].Value;
                    World.EntityManager.SetComponentData(child1, new IntCompo2{Value = Random.Range(-10, 20)});
                    World.EntityManager.SetComponentData(child1, new FloatCompo2{Value = Random.Range(-10f, 20f)});
                    {
                        var b1 = EntityManager.GetBuffer<Buf2>(lg[2].Value);
                        for (int el = 0; el < 5; ++el)
                            b1.ElementAt(el).Value = Random.Range(-10, 20);
                    }
                }
            }
        }
    }
}
