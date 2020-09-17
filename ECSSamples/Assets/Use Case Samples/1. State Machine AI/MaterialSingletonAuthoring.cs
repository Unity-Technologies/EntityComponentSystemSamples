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
[RequiresEntityConversion]
public class MaterialSingletonAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Material m_IdleMaterial;
    public Material m_PatrollingMaterial;
    public Material m_ChasingMaterial;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        #if !UNITY_DISABLE_MANAGED_COMPONENTS
        dstManager.AddComponentData(entity, new StateTransitionMaterials
        {
            m_ChasingMaterial = m_ChasingMaterial,
            m_IdleMaterial = m_IdleMaterial,
            m_PatrollingMaterial = m_PatrollingMaterial
        });
        #endif
    }
}
