using Unity.Entities;
using UnityEngine;

namespace Tutorials.Firefighters
{
    public struct Team : IComponentData
    {
        public Entity Filler;
        public Entity Bucket;
        public int NumFiresDoused;
    }

    // all bots in the team (including the Filler and Douser) in order of passing, starting with the Filler
    public struct TeamMember : IBufferElementData
    {
        public Entity Bot;
    }

    // used as a flag to signal that the team needs to be repositioned
    public struct RepositionLine : IComponentData, IEnableableComponent
    {
    }

    public struct Heat : IBufferElementData
    {
        public float Value;
    }
    
    public class BotAnimation : IComponentData
    {
        public GameObject AnimatedGO;   // the GO that is rendered and animated
    }
}
