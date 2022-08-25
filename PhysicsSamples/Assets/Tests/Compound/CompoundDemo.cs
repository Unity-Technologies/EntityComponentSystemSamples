using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public class CompoundDemoScene : SceneCreationSettings {};

public class CompoundDemo : SceneCreationAuthoring<CompoundDemoScene> {}

public class CompoundDemoSystem : SceneCreationSystem<CompoundDemoScene>
{
    public override void CreateScene(CompoundDemoScene sceneSettings)
    {
        //         // Floor
        //         {
        //             BlobAssetReference<Unity.Physics.Collider> collider = Unity.Physics.BoxCollider.Create(new float3(0, -0.1f, 0), Quaternion.identity, new float3(10.0f, 0.1f, 10.0f), 0.05f);
        //             CreatedColliders.Add(collider);
        //             CreateStaticBody(float3.zero, quaternion.identity, collider);
        //         }

        // Dynamic compound
        {
            var box = new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(1),
                BevelRadius = 0.05f
            };

            var sphere = new SphereGeometry
            {
                Center = float3.zero,
                Radius = 0.5f
            };

            var children = new NativeArray<CompoundCollider.ColliderBlobInstance>(3, Allocator.Temp)
            {
                [0] = new CompoundCollider.ColliderBlobInstance
                {
                    CompoundFromChild = new RigidTransform(quaternion.identity, new float3(-1, 0, 0)),
                    Collider = Unity.Physics.BoxCollider.Create(box)
                },
                [1] = new CompoundCollider.ColliderBlobInstance
                {
                    CompoundFromChild = RigidTransform.identity,
                    Collider = Unity.Physics.SphereCollider.Create(sphere)
                },
                [2] = new CompoundCollider.ColliderBlobInstance
                {
                    CompoundFromChild = new RigidTransform(quaternion.identity, new float3(1, 0, 0)),
                    Collider = Unity.Physics.BoxCollider.Create(box)
                }
            };
            foreach (var child in children)
                CreatedColliders.Add(child.Collider);

            BlobAssetReference<Unity.Physics.Collider> collider = CompoundCollider.Create(children);
            CreatedColliders.Add(collider);
            children.Dispose();

            CreateDynamicBody(new float3(0, 1, 0), quaternion.identity, collider, float3.zero, float3.zero, 1.0f);
        }
    }
}
