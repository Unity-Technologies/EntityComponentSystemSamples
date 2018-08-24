using System.Collections.Generic;
using Data;
using Unity.Entities;
using UnityEngine;

namespace Other
{
    /// <summary>
    /// Auto playing functionality. If it gets run via the SceneSwitcher scene it will auto play by default.
    /// </summary>
    public class AutoPlay : MonoBehaviour
    {
        [SerializeField]
        float attackInterval = 0.1f;
        [SerializeField]
        float attackCountdown = 0.1f;

        GameObject[] planets;
        EntityManager entityManager { get; set; }

        public void Start()
        {
            planets = GameObject.FindGameObjectsWithTag("Planet");
            entityManager = World.Active.GetOrCreateManager<EntityManager>();
            var sceneSwitcher = GameObject.Find("SceneSwitcher");

            // We mostly only want to run the automation when we are running through the scene switcher.
            // Can also be enabled by toggling the AutoPlay script in the Spawners object in the scene
            if (sceneSwitcher == null)
            {
                enabled = false;
            }
        }

        public void Update()
        {
            attackCountdown -= Time.deltaTime;
            if (attackCountdown > 0.0f)
                return;

            attackCountdown = attackInterval;

            if(planets.Length <= 1)
                Debug.LogError("No planets found, what's wrong?");

            var sourcePlanetIndex = Random.Range(0, planets.Length);
            var sourcePlanetEntity = planets[sourcePlanetIndex].GetComponent<GameObjectEntity>().Entity;
            
            if(!entityManager.Exists(sourcePlanetEntity))
            {
                // Can happen during scene unload
                enabled = false;
                return;
            }

            var planetData = PlanetUtility.GetPlanetData(sourcePlanetEntity, entityManager);
            
            while (planetData.TeamOwnership == 0)
            {
                sourcePlanetIndex = Random.Range(0, planets.Length);
                sourcePlanetEntity = planets[sourcePlanetIndex].GetComponent<GameObjectEntity>().Entity;
            
                if(!entityManager.Exists(sourcePlanetEntity))
                {
                    // Can happen during scene unload
                    enabled = false;
                    return;
                }
                planetData = PlanetUtility.GetPlanetData(sourcePlanetEntity, entityManager);
            }

            var targetPlanetIndex = Random.Range(0, planets.Length);

            while (targetPlanetIndex == sourcePlanetIndex)
            {
                targetPlanetIndex = Random.Range(0, planets.Length);
            }

            PlanetUtility.AttackPlanet(planets[sourcePlanetIndex], planets[targetPlanetIndex], entityManager);
        }

    }
}
