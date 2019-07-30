using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System;

// An example character controller that uses a rigid body to move
// See CharacterControllerComponent for a 'proxy' based character controller also

public struct CharacterControllerBody : IComponentData
{
    // From Behavior
    public float MovementSpeed;
    public float MovementSpeedInAir;
    public float RotationSpeed;
    public float JumpSpeed;
    public float CharacterHeight;
    public float3 MovementDamping;
    public float DeadZone;

    // runtime only
    public Entity Entity;
    public Entity GunArmEntity;
    public float3 MovementVector;
    public float RotationAngle;
    public float3 InitialUnsupportedVelocity;
    public bool IsSupported;
    public bool IsJumping;
}

public class CharacterControllerBodyBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    // Input
    public GameObject GunArm;
    public float MovementSpeed = 5;
    public float MovementSpeedInAir = 2.5f;
    public float RotationSpeed = 5;
    public float JumpSpeed = 6;
    public float CharacterHeight = 1.1f;
    public float3 MovementDamping = new float3(0, 1, 0);
    public float DeadZone = 0.1f;

    void OnEnable() { }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            var componentData = new CharacterControllerBody
            {
                MovementSpeed = MovementSpeed,
                MovementSpeedInAir = MovementSpeedInAir,
                RotationSpeed = RotationSpeed,
                JumpSpeed = JumpSpeed,
                CharacterHeight = CharacterHeight,
                MovementDamping = MovementDamping,
                DeadZone = DeadZone,
         
                Entity = entity,
                GunArmEntity = conversionSystem.GetPrimaryEntity(GunArm),
                MovementVector = float3.zero,
                RotationAngle = 0,
                InitialUnsupportedVelocity = float3.zero,
                IsSupported = false,
                IsJumping = false
            };

            dstManager.AddComponentData(entity, componentData);
        }
    }
}

[UpdateBefore(typeof(BuildPhysicsWorld))]
public class CharacterControllerBodySystem : ComponentSystem
{
    protected override unsafe void OnUpdate()
    {
        Entities.ForEach(
            (ref CharacterControllerBody ccBodyComponentData,
            ref PhysicsCollider collider,
            ref PhysicsVelocity velocity,
            ref Translation position,
            ref Rotation rotation) =>
            {
                float3 up = math.up();

                ref PhysicsWorld world = ref World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
                // movement and firing
                {
                    float x = Input.GetAxis("Horizontal");
                    float z = Input.GetAxis("Vertical");
                    ccBodyComponentData.IsJumping = Input.GetButtonDown("Jump") && ccBodyComponentData.IsSupported;

                    bool haveInput = (Mathf.Abs(x) > Mathf.Epsilon) || (Mathf.Abs(z) > Mathf.Epsilon);
                    if (!haveInput)
                    {
                        ccBodyComponentData.MovementVector = float3.zero;
                    }
                    else
                    {
                        float3 movement = math.rotate(quaternion.RotateY(ccBodyComponentData.RotationAngle), new float3(x, 0, z));
                        ccBodyComponentData.MovementVector = math.normalize(movement);
                    }

                    if ((ccBodyComponentData.GunArmEntity != Entity.Null) && EntityManager.HasComponent<Rotation>(ccBodyComponentData.GunArmEntity) )
                    {
                        float a = -Input.GetAxis("ShootY");
                        Rotation gunRot = EntityManager.GetComponentData<Rotation>(ccBodyComponentData.GunArmEntity);
                        gunRot.Value = math.mul(gunRot.Value, quaternion.Euler(math.radians(a), 0, 0));
                        EntityManager.SetComponentData(ccBodyComponentData.GunArmEntity, gunRot);

                        if ( EntityManager.HasComponent<PhysicsGun>(ccBodyComponentData.GunArmEntity) )
                        {
                            var gunFire = EntityManager.GetComponentData<PhysicsGun>(ccBodyComponentData.GunArmEntity);
                            gunFire.isFiring = Input.GetButton("Fire1")? 1 : 0;
                            EntityManager.SetComponentData(ccBodyComponentData.GunArmEntity, gunFire);
                        }
                    }
                }

                // Rotate
                {
                    float x = Input.GetAxis("ShootX");
                    bool haveInput = (Mathf.Abs(x) > Mathf.Epsilon);
                    if (haveInput)
                    {
                        ccBodyComponentData.RotationAngle += x * ccBodyComponentData.RotationSpeed * Time.deltaTime;
                    }

                    rotation.Value = quaternion.AxisAngle(math.up(), ccBodyComponentData.RotationAngle);
                }

                // check supported
                {

                    float3 rayStart = position.Value;
                    float3 rayEnd = rayStart + (ccBodyComponentData.CharacterHeight * -math.up());

                    var rayInput = new RaycastInput
                    {
                        Start = rayStart,
                        End = rayEnd,
                        Filter = collider.Value.Value.Filter,
                    };

                    Unity.Physics.RaycastHit rayHit;
                    bool hit = (world.CastRay(rayInput, out rayHit) && rayHit.SurfaceNormal.y > 0.5);
                    if (ccBodyComponentData.IsSupported && !hit)
                    {
                        ccBodyComponentData.InitialUnsupportedVelocity = velocity.Linear;
                    }
                    ccBodyComponentData.IsSupported = hit;
                }

                // tweak velocity
                //this.MovementVector = new float3(1, 0, 0);
                //this.IsJumping = true;
                {
                    float3 lv = velocity.Linear;
                    lv *= ccBodyComponentData.MovementDamping;
                    bool bHaveMovement = ccBodyComponentData.IsJumping || (math.lengthsq(ccBodyComponentData.MovementVector) > (ccBodyComponentData.DeadZone * ccBodyComponentData.DeadZone));
                    if (bHaveMovement)
                    {
                        float y = lv.y;
                        if (ccBodyComponentData.IsSupported)
                        {
                            lv = ccBodyComponentData.MovementSpeed * ccBodyComponentData.MovementVector;
                            lv.y = y;
                            if (ccBodyComponentData.IsJumping)
                            {
                                lv.y += ccBodyComponentData.JumpSpeed;
                                ccBodyComponentData.IsJumping = false;
                            }
                        }
                        else
                        {
                            ccBodyComponentData.InitialUnsupportedVelocity *= ccBodyComponentData.MovementDamping;
                            ccBodyComponentData.InitialUnsupportedVelocity.y = y;
                            lv = ccBodyComponentData.InitialUnsupportedVelocity + (ccBodyComponentData.MovementSpeed * ccBodyComponentData.MovementVector);
                        }
                    }

                    velocity.Linear = lv;
                    velocity.Angular = float3.zero;
                }
         }); // ForEach
    }
}
