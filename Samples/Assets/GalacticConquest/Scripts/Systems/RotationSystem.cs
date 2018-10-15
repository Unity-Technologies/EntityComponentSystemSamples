using Data;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Systems
{
    /// <summary>
    /// Rotates any entity which has a Transform and a RotationData component, in this sample the planets are
    /// what will be affected by this
    /// </summary>
    public class RotationSystem : JobComponentSystem
    {
#pragma warning disable 649
        struct Planets
        {

            public readonly int Length;

            public ComponentDataArray<RotationData> Data;
            public TransformAccessArray Transforms;
        }
#pragma warning restore 649
        
        struct RotationJob : IJobParallelForTransform
        {
            public ComponentDataArray<RotationData> Rotations;
            public void Execute(int index, TransformAccess transform)
            {
                transform.rotation = transform.rotation * Quaternion.Euler( Rotations[index].RotationSpeed);
            }
        }

        [Inject]
#pragma warning disable 649
        Planets _planets;
#pragma warning restore 649
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new RotationJob
            {
                Rotations = _planets.Data
            };

            return job.Schedule(_planets.Transforms, inputDeps);
        }
    }
}
