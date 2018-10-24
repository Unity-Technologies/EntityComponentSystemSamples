using Data;
using Unity.Entities;
using UnityEngine;

namespace Other
{
    /// <summary>
    /// Just updates the text on the planets to represent the occupant count from the attached PlanetData
    /// </summary>
    public class OccupantsTextUpdater : MonoBehaviour
    {
        Entity _planetEntity;
        TextMesh _text;
        int LastOccupantCount = -1;
        [SerializeField]
        EntityManager _entityManager;

        void Start()
        {
            _entityManager = World.Active.GetOrCreateManager<EntityManager>();
            _planetEntity = transform.parent.GetComponent<GameObjectEntity>().Entity;
            _text = GetComponent<TextMesh>();

        }

        void Update()
        {
            // True when running in the scene switcher and it has started deleting entities before switching scene
            if(!_entityManager.Exists(_planetEntity))
                return;

            var data = _entityManager.GetComponentData<PlanetData>(_planetEntity);
            if (data.Occupants == LastOccupantCount)
                return;
            LastOccupantCount = data.Occupants;
            _text.text = LastOccupantCount.ToString();
        }
    }
}
