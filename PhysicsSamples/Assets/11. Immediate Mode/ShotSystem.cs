using Unity.Entities;
using UnityEngine;

namespace ImmediateMode
{
    public partial struct ShotSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Shot>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ui = ShotUI.Singleton;
            if (ui == null)
            {
                return;
            }

            var shot = SystemAPI.GetSingletonRW<Shot>();

            if (ui.GetSliderVelocity(out var sliderVelocity))
            {
                shot.ValueRW.Velocity = sliderVelocity;
            }

            if (ui.GetClick())
            {
                shot.ValueRW.TakeShot = true;
            }
        }
    }
}
