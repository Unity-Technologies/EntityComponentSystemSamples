using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HelloCube.MySample._03
{
    public class RotationSpeedAuthering : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float DegreesPerSecond = 360.0f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new RotationSpeed() {RadianPerSecond = math.radians(DegreesPerSecond)};
            dstManager.AddComponentData(entity, data);
        }
    }
}