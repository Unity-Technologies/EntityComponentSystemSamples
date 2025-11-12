using UnityEngine;
using Unity.Entities;

#if !UNITY_DISABLE_MANAGED_COMPONENTS
namespace Boids
{
    /*
     * This file ensures that we preserve the current typemanager hashing behavior, 
     * which is that if you #if UNITY_EDITOR a field in a class IComponentData itself,
     * it will have a different hash in the player and then there will be errors 
     * when the player runs if you put it in a subscene. 
     * 
     * But, if you do the same thing on a class type that the class IComponentData 
     * includes as a field, it will be fine, because we don't look inside class types
     * of fields. 
     */ 
    public class MMCWIFAuthoring : MonoBehaviour
    {
        class Baker : Baker<MMCWIFAuthoring>
        {
            public override void Bake(MMCWIFAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new MyManagedComponentWithIfdeffedField());
            }
        }
    }
    public class MemberThingWithIfdeffedField
    {
#if UNITY_EDITOR
        public int x;
#endif
    }


    public class MyManagedComponentWithIfdeffedField : IComponentData
    {
        MemberThingWithIfdeffedField x;
    }
}
#endif
