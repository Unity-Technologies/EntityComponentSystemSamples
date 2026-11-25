#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL && GAMEOBJECTSAMPLE_MODULE_PHYSICSCORE2D
using System.Collections.Generic;
using Unity.Netcode;
using Unity.U2D.Physics;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // Static scene geometry (floor, walls) for PhysicsCore2D. Not a ghost: it adds a copy of itself to every
    // netcode-managed world, the way a BoxCollider2D in the scene is shared by every world's ghosts.
    public class CoreStaticBox2D : MonoBehaviour
    {
        [SerializeField] float m_Friction = 0.6f;

        readonly List<PhysicsBody> m_Bodies = new List<PhysicsBody>();

        void OnEnable()
        {
            GhostPhysicsCore2DWorlds.WorldCreated += AddTo;
            foreach (var world in GhostPhysicsCore2DWorlds.Worlds)
                AddTo(world);
        }

        void OnDisable()
        {
            GhostPhysicsCore2DWorlds.WorldCreated -= AddTo;
            foreach (var body in m_Bodies)
            {
                if (body.isValid)
                    body.Destroy();
            }
            m_Bodies.Clear();
        }

        void AddTo(PhysicsWorld world)
        {
            transform.GetPositionAndRotation(out var position, out var rotation);
            var bodyDefinition = PhysicsBodyDefinition.defaultDefinition;
            bodyDefinition.type = PhysicsBody.BodyType.Static;
            bodyDefinition.position = position;
            bodyDefinition.rotation = PhysicsRotate.FromRadians(PhysicsMath.ToRotation2D(rotation));
            var body = world.CreateBody(bodyDefinition);

            var shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            var material = shapeDefinition.surfaceMaterial;
            material.friction = m_Friction;
            shapeDefinition.surfaceMaterial = material;
            var scale = transform.lossyScale;
            body.CreateShape(PolygonGeometry.CreateBox(new Vector2(scale.x, scale.y)), shapeDefinition);

            m_Bodies.Add(body);
        }
    }
}
#endif
