using Data;
using Unity.Collections;
using Unity.Entities;

namespace Systems
{
    /// <summary>
    /// Handles when a ship has arrived at it's target planet (all ships that have the ShipArrivedTag component on them)
    /// This could be to either reinforce the planet or to attack it.
    /// When the occupants of a planet of a different team reaches 0 it will switch the planet to the team of the ship
    /// 
    /// </summary>
    public class ShipArrivalSystem : ComponentSystem
    {
        EntityManager _entityManager;

        public ShipArrivalSystem()
        {
            _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        }
        
#pragma warning disable 649
        struct Ships
        {

            public readonly int Length;

            public ComponentDataArray<ShipData> Data;
            public EntityArray Entities;
            public ComponentDataArray<ShipArrivedTag> Tag;
        }
        [Inject]
        Ships _ships;
#pragma warning restore 649

        protected override void OnUpdate()
        {
            if (_ships.Length == 0)
                return;

            var arrivingShipTransforms = new NativeList<Entity>(Allocator.Temp);
            var arrivingShipData = new NativeList<ShipData>(Allocator.Temp);

            for (var shipIndex = 0; shipIndex < _ships.Length; shipIndex++)
            {
                var shipData = _ships.Data[shipIndex];
                var shipEntity = _ships.Entities[shipIndex];
                arrivingShipData.Add(shipData);
                arrivingShipTransforms.Add(shipEntity);
            }

            HandleArrivedShips(arrivingShipData, arrivingShipTransforms);

            arrivingShipTransforms.Dispose();
            arrivingShipData.Dispose();
        }

        void HandleArrivedShips(NativeList<ShipData> arrivingShipData, NativeList<Entity> arrivingShipEntities)
        {
            for (var shipIndex = 0; shipIndex < arrivingShipData.Length; shipIndex++)
            {

                var shipData = arrivingShipData[shipIndex];
                var planetData = _entityManager.GetComponentData<PlanetData>(shipData.TargetEntity);

                if (shipData.TeamOwnership != planetData.TeamOwnership)
                {
                    planetData.Occupants = planetData.Occupants - 1;
                    if (planetData.Occupants <= 0)
                    {
                        planetData.TeamOwnership = shipData.TeamOwnership;
                        PlanetSpawner.SetColor(shipData.TargetEntity, planetData.TeamOwnership);
                    }
                }
                else
                {
                    planetData.Occupants = planetData.Occupants + 1;
                }
                _entityManager.SetComponentData(shipData.TargetEntity, planetData);
            }
            _entityManager.DestroyEntity(arrivingShipEntities);
        }
    }
}
