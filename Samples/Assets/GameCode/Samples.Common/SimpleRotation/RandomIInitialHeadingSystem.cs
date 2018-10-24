using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Samples.Common
{
    public class RandomInitialHeadingSystem : ComponentSystem
    {
        struct RandomInitialHeadingGroup
        {
#pragma warning disable 649
            [ReadOnly] public ComponentDataArray<RandomInitialHeading> RandomInitialHeadiings;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<Heading> Headings;
            public readonly int Length;
#pragma warning restore 649
        }

        [Inject] RandomInitialHeadingGroup m_Group;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Group.Length; i++)
            {
                m_Group.Headings[i] = new Heading
                {
                    Value = math.normalize(new float3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1,1), UnityEngine.Random.Range(-1, 1)))
                };
                
                PostUpdateCommands.RemoveComponent<RandomInitialHeading>(m_Group.Entities[i]);
            }
        }
    }
}
