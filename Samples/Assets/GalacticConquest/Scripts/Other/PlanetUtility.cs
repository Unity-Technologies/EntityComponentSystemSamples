using System.Linq;
using Data;
using Unity.Entities;
using UnityEngine;

namespace Other
{
    /// <summary>
    /// Some shared functionality between AutoPlay and UserInputSystem
    /// </summary>
    public static class PlanetUtility
    {
        public static void AttackPlanet(GameObject fromPlanet, GameObject toPlanet, EntityManager entityManager)
        {
            var entity = fromPlanet.GetComponent<GameObjectEntity>().Entity;
            var meshComponent = fromPlanet.GetComponentsInChildren<GameObjectEntity>().First(c => c.gameObject != fromPlanet.gameObject);
            var occupantData = entityManager.GetComponentData<PlanetData>(entity);
            var targetEntity = toPlanet.GetComponent<GameObjectEntity>().Entity;
            var launchData = new PlanetShipLaunchData
            {
                TargetEntity = targetEntity,
                TeamOwnership = occupantData.TeamOwnership,
                NumberToSpawn = occupantData.Occupants,
                SpawnLocation = fromPlanet.transform.position,
                SpawnRadius = meshComponent.transform.lossyScale.x * 0.5f
            };

            occupantData.Occupants = 0;
            entityManager.SetComponentData(entity, occupantData);
            if (entityManager.HasComponent<PlanetShipLaunchData>(entity))
            {
                entityManager.SetComponentData(entity, launchData);
                return;
            }
            entityManager.AddComponentData(entity, launchData);
        }

        public static PlanetData GetPlanetData (GameObject planet, EntityManager entityManager)
        {
            var entity = planet.GetComponent<GameObjectEntity>().Entity;
            var data = GetPlanetData(entity, entityManager);
            return data;
        }

        public static PlanetData GetPlanetData(Entity entity, EntityManager entityManager)
        {
            return entityManager.GetComponentData<PlanetData>(entity);
        }
    }
}
