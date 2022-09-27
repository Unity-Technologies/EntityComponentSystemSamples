using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#if !UNITY_DISABLE_MANAGED_COMPONENTS
class StateTransitionMaterials : IComponentData
{
    public Material m_IdleMaterial;
    public Material m_PatrollingMaterial;
    public Material m_ChasingMaterial;
}
#endif

[DisallowMultipleComponent]
public class MaterialSingletonAuthoring : MonoBehaviour
{
    public Material m_IdleMaterial;
    public Material m_PatrollingMaterial;
    public Material m_ChasingMaterial;

    class Baker : Baker<MaterialSingletonAuthoring>
    {
        public override void Bake(MaterialSingletonAuthoring authoring)
        {
            #if !UNITY_DISABLE_MANAGED_COMPONENTS
            AddComponentObject( new StateTransitionMaterials
            {
                m_ChasingMaterial = authoring.m_ChasingMaterial,
                m_IdleMaterial = authoring.m_IdleMaterial,
                m_PatrollingMaterial = authoring.m_PatrollingMaterial
            });
            #endif
        }
    }
}
