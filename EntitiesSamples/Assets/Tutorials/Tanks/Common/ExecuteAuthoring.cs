using Unity.Entities;
using UnityEngine;

namespace Tutorials.Tanks.Execute
{
    public class ExecuteAuthoring : MonoBehaviour
    {
        [Header("Step 2")]
        public bool TurretRotation;

        [Header("Step 3")]
        public bool TankMovement;

        [Header("Step 4")]
        public bool TurretShooting;

        [Header("Step 5")]
        public bool CannonBall;

        [Header("Step 6")]
        public bool TankSpawning;

        [Header("Step 7")]
        public bool SafeZone;

        [Header("Step 8")]
        public bool Camera;

        class Baker : Baker<ExecuteAuthoring>
        {
            public override void Bake(ExecuteAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (authoring.TurretRotation) AddComponent<TurretRotation>(entity);
                if (authoring.TankMovement) AddComponent<TankMovement>(entity);
                if (authoring.TurretShooting) AddComponent<TurretShooting>(entity);
                if (authoring.CannonBall) AddComponent<CannonBall>(entity);
                if (authoring.TankSpawning) AddComponent<TankSpawning>(entity);
                if (authoring.SafeZone) AddComponent<SafeZone>(entity);
                if (authoring.Camera) AddComponent<Camera>(entity);
            }
        }
    }

    public struct TurretRotation : IComponentData
    {
    }

    public struct TankMovement : IComponentData
    {
    }

    public struct TurretShooting : IComponentData
    {
    }

    public struct CannonBall : IComponentData
    {
    }

    public struct Camera : IComponentData
    {
    }

    public struct SafeZone : IComponentData
    {
    }

    public struct TankSpawning : IComponentData
    {
    }
}
