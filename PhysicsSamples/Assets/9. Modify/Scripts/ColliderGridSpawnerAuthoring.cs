// This script is used in the 5g1. Change Collision Filter - Boxes demo.
// Baker for the ColliderGridSpawner where the component is used in the ColliderGridCreationSystem.
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics
{
    public class ColliderGridSpawner : MonoBehaviour
    {
        public GameObject Prefab_BuiltInUniqueComponent;
        public GameObject Prefab_PhysicsShapeUniqueToggle;
        public GameObject Prefab_PhysicsShapeUniqueComponent;
        public UnityEngine.Material MaterialGrow;
        public UnityEngine.Material MaterialShrink;

        class ColliderGridSpawnerBaker : Baker<ColliderGridSpawner>
        {
            public override void Bake(ColliderGridSpawner authoring)
            {
                DependsOn(authoring.Prefab_BuiltInUniqueComponent);
                if (authoring.Prefab_BuiltInUniqueComponent == null || authoring.Prefab_PhysicsShapeUniqueToggle == null) return;

                var prefabBuiltInEntity = GetEntity(authoring.Prefab_BuiltInUniqueComponent, TransformUsageFlags.Dynamic);
                var prefabPhysicsShapeToggleEntity = GetEntity(authoring.Prefab_PhysicsShapeUniqueToggle, TransformUsageFlags.Dynamic);
                var prefabPhysicsShapeComponentEntity = GetEntity(authoring.Prefab_PhysicsShapeUniqueComponent, TransformUsageFlags.Dynamic);

                var materialGrow = authoring.MaterialGrow;
                var materialShrink = authoring.MaterialShrink;

                var createComponent = new CreateColliderGridComponent
                {
                    BuiltInEntity = prefabBuiltInEntity,
                    PhysicsShapeToggleEntity = prefabPhysicsShapeToggleEntity,
                    PhysicsShapeComponentEntity = prefabPhysicsShapeComponentEntity,
                    SpawningPosition = authoring.transform.position
                };
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, createComponent);

                AddSharedComponentManaged(entity, new ColliderMaterialsComponent
                {
                    GrowMaterial = materialGrow,
                    ShrinkMaterial = materialShrink
                });
            }
        }
    }

    public struct CreateColliderGridComponent : IComponentData
    {
        public Entity BuiltInEntity;
        public Entity PhysicsShapeToggleEntity;
        public Entity PhysicsShapeComponentEntity;
        public float3 SpawningPosition;
    }

    public struct ColliderMaterialsComponent : ISharedComponentData, IEquatable<ColliderMaterialsComponent>
    {
        public UnityEngine.Material GrowMaterial;
        public UnityEngine.Material ShrinkMaterial;

        public bool Equals(ColliderMaterialsComponent other) =>
            Equals(GrowMaterial, other.GrowMaterial)
            && Equals(ShrinkMaterial, other.ShrinkMaterial);

        public override bool Equals(object obj) => obj is ColliderMaterialsComponent other && Equals(other);

        public override int GetHashCode() =>
            unchecked((int)math.hash(new int2(
                GrowMaterial != null ? GrowMaterial.GetHashCode() : 0,
                ShrinkMaterial != null ? ShrinkMaterial.GetHashCode() : 0
            )));
    }
}
