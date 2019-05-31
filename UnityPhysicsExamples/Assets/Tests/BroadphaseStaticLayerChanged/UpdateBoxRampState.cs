using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct EntityUpdater : IComponentData
{
    public int TimeToDie;
    public int TimeToMove;
}

public class UpdateBoxRampState : MonoBehaviour, IConvertGameObjectToEntity
{
    public int TimeToDie;
    public int TimeToMove;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new EntityUpdater { TimeToDie = TimeToDie, TimeToMove = TimeToMove });
    }
}

public class EntityUpdaterSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach(
            (Entity entity, ref EntityUpdater updater, ref Translation position) =>
            {
                if (updater.TimeToDie-- == 0)
                {
                    PostUpdateCommands.DestroyEntity(entity);
                }

                if (updater.TimeToMove-- == 0)
                {
                    position.Value += new float3(0, -2, 0);
                    PostUpdateCommands.SetComponent(entity, position);
                }
            }
        );
    }
}
