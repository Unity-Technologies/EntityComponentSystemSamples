using Unity.Entities;

namespace Tutorials.Firefighters
{
    public struct Team : IComponentData
    {
        public Entity Filler;
        public Entity Douser;
        public bool HasBucket;
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
}
