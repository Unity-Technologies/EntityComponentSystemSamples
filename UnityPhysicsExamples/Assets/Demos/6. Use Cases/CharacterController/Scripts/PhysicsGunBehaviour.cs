using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;
using System.Collections.Generic;
using System;
using Unity.Physics;

public struct PhysicsGun: IComponentData
{
    public Entity bullet;
    public float strength;
    public float rate;
    public float duration;

    public int wasFiring;
    public int isFiring;
}


public class PhysicsGunBehaviour : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject bullet;

    public float strength;
    public float rate;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(bullet);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<PhysicsGun>(
            entity,
            new PhysicsGun()
            {
                bullet = conversionSystem.GetPrimaryEntity(bullet),
                strength = strength,
                rate = rate,
                wasFiring = 0,
                isFiring = 0
            });
    }
}


#region System
// update before physics gets going so that we dont have hazzard warnings
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class PhysicsGunSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        float dt = Time.fixedDeltaTime;
        
        Entities.ForEach(
            (ref LocalToWorld gunTransform, ref Rotation gunRotation, ref PhysicsGun gun) =>
            {
                if (gun.isFiring == 0)
                {
                    gun.duration = 0;
                    gun.wasFiring = 0;
                    return;
                }

                gun.duration += dt;
                if ( (gun.duration > gun.rate) || (gun.wasFiring == 0) )
                {
                    if (gun.bullet != null)
                    {
                        var e = PostUpdateCommands.Instantiate(gun.bullet);

                        Translation position = new Translation() { Value = gunTransform.Position + gunTransform.Forward };
                        Rotation rotation = new Rotation() { Value = gunRotation.Value };
                        PhysicsVelocity velocity = new PhysicsVelocity()
                        {
                            Linear = gunTransform.Forward * gun.strength,
                            Angular = float3.zero
                        };

                        PostUpdateCommands.SetComponent(e, position);
                        PostUpdateCommands.SetComponent(e, rotation);
                        PostUpdateCommands.SetComponent(e, velocity);
                    }
                    gun.duration = 0;
                }
                gun.wasFiring = 1;
            }
        );
    }
}
#endregion