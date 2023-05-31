using Unity.Entities;
using UnityEngine;

namespace ExampleCode.Bakers
{
    // An example component for which we want to
    // define an authoring component and a baker.
    public struct EnergyShield : IComponentData
    {
        public int HitPoints;
        public int MaxHitPoints;
        public float RechargeDelay;
        public float RechargeRate;
    }

    // An authoring component for EnergyShield.
    // By itself, an authoring component is just an ordinary MonoBehavior.
    public class EnergyShieldAuthoring : MonoBehaviour
    {
        // Notice the authoring component has no HitPoints field.
        // This is fine as long as we don't need to set the HitPoints
        // value in the editor.

        // (The fact that these names mirror the fields
        // of EnergyShield is not a requirement.)

        public int MaxHitPoints;
        public float RechargeDelay;
        public float RechargeRate;

        // The baker for our EnergyShield authoring component.
        // For every GameObject in an entity subscene, baking creates a
        // corresponding entity. This baker is run once for every
        // EnergyShieldAuthoring instance that's attached to any GameObject in
        // the entity subscene.
        public class Baker : Baker<EnergyShieldAuthoring>
        {
            public override void Bake(EnergyShieldAuthoring authoring)
            {
                // The TransformUsageFlags specifies which transform components the
                // entity should have. 'None' means that it doesn't need any.
                var entity = GetEntity(TransformUsageFlags.None);

                // This simple baker adds just one component to the entity.
                AddComponent(entity, new EnergyShield
                {
                    HitPoints = authoring.MaxHitPoints,
                    MaxHitPoints = authoring.MaxHitPoints,
                    RechargeDelay = authoring.RechargeDelay,
                    RechargeRate = authoring.RechargeRate,
                });
            }
        }
    }

    /*
     *  A Baker must register the data it accesses. For the authoring component, this is automatic, but for other
     *  data (assets, prefabs, and other GameObject components), you must access them through
     *  Baker methods to ensure the Baker is aware of them:
     */

    public struct MyComponent : IComponentData
    {
        public int A;
        public float B;
        public Entity Prefab;
    }

    public class MyAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public GameObject otherGO;
        public Mesh mesh;

        public class Baker : Baker<MyAuthoring>
        {
            public override void Bake(MyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // To do this, use the functions that the Baker provides to access other components
                // instead of the ones provided by GameObject. In the same way, if you access data from an asset,
                // you need to create a dependency for it, so the Baker reruns if the asset changes.
                // We want to re-bake if anything changes in the mesh itself.

                var transform = GetComponent<Transform>(authoring.otherGO);
                DependsOn(authoring.mesh);

                AddComponent(entity, new MyComponent
                {
                    A = authoring.mesh.vertexCount,
                    B = transform.localPosition.x,
                    // to register and convert Prefabs, call `GetEntity` in the baker:
                    Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
