using Common.Scripts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Joints
{
    public class Ragdoll : SceneCreationSettings
    {
        public Mesh TorsoMesh;
        public Mesh RenderMesh;
        public int NumberOfRagdolls;
        public float RangeGain;
        public RigidTransform Transform;
    }

    public class RagdollAuthoring : SceneCreationAuthoring<Ragdoll>
    {
        public Mesh TorsoMesh;
        public Mesh RenderMesh;
        public int NumberOfRagdolls = 1;
        [Range(0, 1)] public float RangeGain = 1.0f;

        class Baker : Baker<RagdollAuthoring>
        {
            public override void Bake(RagdollAuthoring authoring)
            {
                DependsOn(authoring.RenderMesh);
                DependsOn(authoring.TorsoMesh);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new Ragdoll
                {
                    DynamicMaterial = authoring.DynamicMaterial,
                    StaticMaterial = authoring.StaticMaterial,
                    RenderMesh = authoring.RenderMesh,
                    TorsoMesh = authoring.TorsoMesh,
                    NumberOfRagdolls = authoring.NumberOfRagdolls,
                    RangeGain = authoring.RangeGain,
                    Transform = new RigidTransform(authoring.transform.rotation, authoring.transform.position)
                });
            }
        }
    }
}
