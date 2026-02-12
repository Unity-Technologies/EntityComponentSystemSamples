using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.NetCode.HostMigration;
using System;

namespace Asteroids.Server
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateBefore(typeof(RpcSystem))]
    public partial struct RegisterMigrationTypesSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var migrationTypesEntity = state.EntityManager.CreateSingletonBuffer<NonGhostMigrationComponents>();
            var migreationTypesBuffer = state.EntityManager.GetBuffer<NonGhostMigrationComponents>(migrationTypesEntity, false);

            migreationTypesBuffer.Add( new NonGhostMigrationComponents(){ ComponentToMigrate = ComponentType.ReadOnly<ServerSettings>() } );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}
