using Unity.Entities;
using UnityEngine;

#if !UNITY_DISABLE_MANAGED_COMPONENTS
[assembly: RegisterUnityEngineComponentType(typeof(Animator))]

public partial struct ChangeRotationAnimationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var animator in SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<Animator>>())
        {
            var sineSpeed = 1f + Mathf.Sin(Time.time);
            animator.Value.speed = sineSpeed;
        }
    }
}
#endif
