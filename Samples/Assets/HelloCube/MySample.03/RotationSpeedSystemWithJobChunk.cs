using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace HelloCube.MySample._03
{
    public class RotationSpeedSystemWithJobChunk : JobComponentSystem
    {
        private EntityQuery m_Group;

        /// <summary>
        /// ジョブシステムの生成時に呼ばれる
        /// </summary>
        protected override void OnCreate()
        {
            Debug.Log("OnCreate");
            // クエリをキャッシュする
            //  ComponentDataの組み合わせを指定する.
            //  ComponentType.Excludeなら指定の型を持っていないが条件として追加される
            m_Group = GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed>());
        }

        // 回転を実行するジョブ
        //   ArchetypeChunk = 任意のComponentData 1インスタンス(実体)
        //   ArchetypeChunkComponentType<T> = ComponentDataのGeneric版(Boxing防止のためのテンプレート)
        //   
        struct RotationSpeedJob : IJobChunk
        {
            public float DeltaTime;
            public ArchetypeChunkComponentType<Rotation> RotationType;
            [ReadOnly] public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                // 指定の型でQueryを
                var chunkRotations = chunk.GetNativeArray(RotationType);
                var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var rotation = chunkRotations[i];
                    var rotationSpeed = chunkRotationSpeeds[i];

                    chunkRotations[i] = new Rotation
                    {
                        Value = math.mul(math.normalize(rotation.Value),
                            quaternion.AxisAngle(math.up(), rotationSpeed.RadianPerSecond * DeltaTime))
                    };
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // 明示的に宣言
            //  - Rotation : Read-Writeアクセス
            //  - RotationSpeed : Read-Onlyアクセス
            var rotationType = GetArchetypeChunkComponentType<Rotation>();
            var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>();

            var job = new RotationSpeedJob()
            {
                RotationType = rotationType,
                RotationSpeedType = rotationSpeedType,
                DeltaTime = Time.deltaTime
            };

            return job.Schedule(m_Group, inputDeps);
        }
    }
}