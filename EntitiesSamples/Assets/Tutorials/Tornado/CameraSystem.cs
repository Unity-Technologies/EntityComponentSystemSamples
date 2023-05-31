using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Tutorials.Tornado
{
    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct CameraSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var tornadoPosition = BuildingSystem.Position((float)SystemAPI.Time.ElapsedTime);
            var cam = Camera.main.transform;
            cam.position = new Vector3(tornadoPosition.x, 10f, tornadoPosition.y) - cam.forward * 60f;
        }
    }
}
