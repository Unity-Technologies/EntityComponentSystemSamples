#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    public enum BodyMode
    {
        Predicted,
        Interpolated,
    }

    // Handle on a ghost's body, so the shared player/projectile code doesn't depend on the physics API.
    // Prediction, rollback and stepping are the netcode package's job (GhostPhysicsCore2DBody).
    public interface ISampleBody2D
    {
        Vector2 LinearVelocity { get; set; }

        // Degrees per second, anticlockwise.
        float AngularVelocity { get; set; }

        void AddImpulse(Vector2 impulse);
    }

    // State shared between the scene, the ghosts and the panel. Static because the panel, the spawners and the
    // ghosts of every world in this process all read it.
    public static class Physics2DSample
    {
        // Systems are global, so the ones in this sample check this before touching anything.
        public static bool IsActive;

        public static BodyMode LocalPlayerMode = BodyMode.Predicted;
        public static BodyMode RemotePlayerMode = BodyMode.Predicted;
        public static BodyMode BoxMode = BodyMode.Predicted;

        public static readonly List<PlayerController2D> Players = new List<PlayerController2D>();
        public static readonly List<GhostBehaviour> Boxes = new List<GhostBehaviour>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsActive = false;
            LocalPlayerMode = BodyMode.Predicted;
            RemotePlayerMode = BodyMode.Predicted;
            BoxMode = BodyMode.Predicted;
            Players.Clear();
            Boxes.Clear();
        }
    }
}
#endif
