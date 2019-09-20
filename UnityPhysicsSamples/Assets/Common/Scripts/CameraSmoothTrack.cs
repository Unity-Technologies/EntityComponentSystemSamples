using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Entities;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

// Camera Utility to smoothly track a specified target from a specified location
// Camera location and target are interpolated each frame to remove overly sharp transitions
public class CameraSmoothTrack : MonoBehaviour
{
    public GameObject Target;
    public GameObject LookTo;
    [Range(0,1)] public float LookToInterpolateFactor = 0.9f;

    public GameObject LookFrom;
    [Range(0, 1)] public float LookFromInterpolateFactor = 0.9f;

    private Vector3 oldPositionTo;

    // Start is called before the first frame update
    void Start()
    {
        if (LookTo == null)
        {
            oldPositionTo = (gameObject.transform.position + Vector3.forward);
        }
        else
        {

            oldPositionTo = LookTo.transform.position;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!enabled) return;

        Vector3 newPositionFrom = (LookFrom == null) ? gameObject.transform.position : LookFrom.transform.position;
        Vector3 newPositionTo = Vector3.forward;
        if (LookTo == null)
        {
            newPositionTo = gameObject.transform.rotation * newPositionTo;
        }
        else
        {
            newPositionTo = LookTo.transform.position;
        }

        PhysicsWorld world = BasePhysicsDemo.DefaultWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
        // check barrier
        {
            var rayInput = new RaycastInput
            {
                Start = newPositionFrom,
                End = newPositionTo,
                Filter = CollisionFilter.Default
            };

            if (world.CastRay(rayInput, out RaycastHit rayResult))
            {
                newPositionFrom = rayResult.Position;
            }
        }

        if (Target != null)
        {
            // add velocity
            var entityManager = BasePhysicsDemo.DefaultWorld.EntityManager;
            var goEntity = Target.GetComponent<GameObjectEntity>();
            if ((entityManager != null) && (goEntity != null) && (goEntity.Entity != Entity.Null))
            {
                Vector3 lv = world.GetLinearVelocity(world.GetRigidBodyIndex(goEntity.Entity));
                lv *= Time.fixedDeltaTime;
                newPositionFrom += lv;
                newPositionTo += lv;
            }
        }

        newPositionFrom = Vector3.Lerp(gameObject.transform.position, newPositionFrom, LookFromInterpolateFactor);
        newPositionTo = Vector3.Lerp(oldPositionTo, newPositionTo, LookToInterpolateFactor);

        Vector3 newForward = newPositionTo - newPositionFrom;
        newForward.Normalize();
        Quaternion newRotation = Quaternion.LookRotation(newForward, Vector3.up);

        gameObject.transform.SetPositionAndRotation(newPositionFrom, newRotation);
        oldPositionTo = newPositionTo;
    }
}
