using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

#if UNITY_EDITOR
[RequiresEntityConversion]
[ConverterVersion("joe", 1)]
public class RotationSpeedFromBuildSettings_IJobChunk : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var rotationSpeedSetting = conversionSystem.GetBuildSettingsComponent<RotationSpeedSetting>();
        
        // Change rotation speed
        var data = new RotationSpeed_IJobChunk { RadiansPerSecond = math.radians(rotationSpeedSetting.RotationSpeed) };
        dstManager.AddComponentData(entity, data);
        
        // Offset the translation of the generated object
        var translation = dstManager.GetComponentData<Translation>(entity);
        translation.Value.y += rotationSpeedSetting.Offset;
        dstManager.SetComponentData(entity, translation);
    }
}
#endif
