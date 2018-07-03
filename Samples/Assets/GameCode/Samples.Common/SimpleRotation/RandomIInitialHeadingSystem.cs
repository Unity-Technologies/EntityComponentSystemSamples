using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Common
{
    public class RandomInitialHeadingSystem : ComponentSystem
    {
        struct RandomInitialHeadingGroup
        {
            [ReadOnly] public ComponentDataArray<RandomInitialHeading> RandomInitialHeadiings;
            [ReadOnly] public EntityArray Entities;
            public ComponentDataArray<Heading> Headings;
            public readonly int Length;
        }

        [Inject] RandomInitialHeadingGroup m_Group;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Group.Length; i++)
            {
                m_Group.Headings[i] = new Heading
                {
                    Value = math.normalize(new float3(Random.Range(-1, 1), Random.Range(-1,1), Random.Range(-1, 1)))
                };
                
                PostUpdateCommands.RemoveComponent<RandomInitialHeading>(m_Group.Entities[i]);
            }
        }
    }
}
