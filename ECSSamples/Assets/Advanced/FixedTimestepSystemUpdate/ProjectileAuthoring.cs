using Unity.Entities;
using UnityEngine;

namespace Samples.FixedTimestepSystem.Authoring
{
    [RequiresEntityConversion]
    [AddComponentMenu("DOTS Samples/FixedTimestepWorkaround/Projectile Spawn Time")]
    [ConverterVersion("joe", 1)]
    public class ProjectileAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Projectile>(entity);
        }
    }
}
