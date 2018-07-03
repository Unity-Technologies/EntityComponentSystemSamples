using System.Collections.Generic;
using System.Linq;
using Data;
using Other;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    /// <summary>
    /// Handles the manual input of the sample.
    /// You left click to select a planet to send from (only team owned planets).
    /// Then you right click to send all the ships from the planet you selected to the planets you right clicked on
    /// Clicking outside deselects all planets
    /// </summary>
    public class UserInputSystem : ComponentSystem
    {
        Dictionary<GameObject, PlanetData?> FromTargets = new Dictionary<GameObject, PlanetData?>();
        GameObject ToTarget = null;

        EntityManager _entityManager;

        public UserInputSystem ()
        {
            _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        }

        protected override void OnUpdate () {
            if (Input.GetMouseButtonDown(0))
            {
                var planet = GetPlanetUnderMouse();
                if (planet == null)
                {
                    FromTargets.Clear();
                    Debug.Log("Clicked outside, so we cleared the from selection");
                }
                else
                {
                    if (FromTargets.ContainsKey(planet))
                    {
                        Debug.Log("Deselecting from target " + planet.name);
                        FromTargets.Remove(planet);
                    }
                    else
                    {
                        var data = PlanetUtility.GetPlanetData(planet, _entityManager);

                        var previousTarget = FromTargets.Values.FirstOrDefault();
                        if ((previousTarget == null || previousTarget.Value.TeamOwnership == data.TeamOwnership) && data.TeamOwnership != 0)
                        {
                            Debug.Log("Selecting from target " + planet.name);
                            FromTargets[planet] = data;
                        }
                        else
                        {
                            if (data.TeamOwnership == 0)
                            {
                                Debug.LogWarning("You cant set a netural planet as a from planet");
                            }
                            else
                            {
                                Debug.Log("Adding planet to from target, but clearing the previous list since it is of a different team");
                                FromTargets.Clear();
                                FromTargets[planet] = data;
                            }
                        }

                    }
                }

            }
            if (Input.GetMouseButtonDown(1))
            {
                var planet = GetPlanetUnderMouse();
                if (planet == null)
                {
                    Debug.Log("Deselecting to target ");
                    ToTarget = null;
                }
                else
                {
                    if (!FromTargets.Any())
                    {
                        Debug.Log("No planets selected to send from, skipping");
                        return;
                    }
                    Debug.Log("Setting To target to " + planet.name);
                    ToTarget = planet;
                    foreach (var p in FromTargets.Keys)
                    {
                        if (p == ToTarget)
                            continue;
                        PlanetUtility.AttackPlanet(p, ToTarget, _entityManager);

                    }
                }
            }
        }

        GameObject GetPlanetUnderMouse()
        {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("Planet")))
            {
                return hit.collider.transform.gameObject;
            }
            return null;
        }
    }
}
