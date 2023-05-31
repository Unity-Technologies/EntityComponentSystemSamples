using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Modify
{
    [RequireComponent(typeof(PhysicsBodyAuthoring))]
    [DisallowMultipleComponent]
    public class NarrowphaseContactsAuthoring : MonoBehaviour
    {
        // SurfaceUpNormal used for non-mesh surfaces.
        // For mesh surface we get the normal from the individual polygon
        public Vector3 SurfaceUpNormal = Vector3.up;

        void OnEnable()
        {
        }

        class Baker : Baker<NarrowphaseContactsAuthoring>
        {
            public override void Bake(NarrowphaseContactsAuthoring authoring)
            {
                if (authoring.enabled)
                {
                    var transform = GetComponent<Transform>();
                    var entity = GetEntity(TransformUsageFlags.Dynamic);
                    AddComponent(entity, new NarrowphaseContacts
                    {
                        surfaceEntity = GetEntity(TransformUsageFlags.Dynamic),
                        surfaceNormal = transform.rotation * authoring.SurfaceUpNormal
                    });
                }
            }
        }
    }

    public struct NarrowphaseContacts : IComponentData
    {
        public Entity surfaceEntity;
        public float3 surfaceNormal;
    }
}
