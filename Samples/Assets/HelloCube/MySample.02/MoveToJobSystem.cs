using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace HelloCube.MySample._02
{
    /// <summary>
    /// MySample.01で作成したMoveToのジョブシステム版
    /// </summary>
    public class MoveToJobSystem : JobComponentSystem
    {
        // 爆速につき推奨!
        // この属性を付与することでバーストコンパイラを使用します. 
        [BurstCompile]
        struct MoveToJob : IJobForEach<Translation, MoveTo>
        {
            // 重要!
            // Time.DeltaTimeはメインスレッド以外で呼び出すとエラーで止まるため
            // 使う場合は、メインスレッドからコピーして使用すること
            public float DeltaTime;
            
            public void Execute(ref Translation translation, [ReadOnly] ref MoveTo moveTo)
            {
                // 現在位置の取得. (float3のSmoothメソッドがないので、Vector3に変換)
                var pos = translation.Value.ToVector3();

                // SmoothDampによる移動処理
                // デフォルトのSmoothDampは内部でTime.DeltaTimeを参照しているため
                // サブスレッドで実行するとハングすることに注意! (今回は、メインスレッドからコピーしたDeltaTimeを使用している)
                // TODO Simulator.DeltaTimeを使用すること
                var newPos = Vector3.SmoothDamp(pos, moveTo.to, ref moveTo.velocity, moveTo.smoothTime, moveTo.speed, DeltaTime);

                // 位置のセット. (Vector3からfloat3に変換)
                translation.Value = newPos.ToFloat3();                
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveToJob
            {
                DeltaTime = Time.deltaTime
            };

            return job.Schedule(this, inputDeps);
        }
    }
    
    public static class Vector3Extensions
    {
        public static float3 ToFloat3(this Vector3 self)
        {
            return new float3(self.x, self.y, self.z);
        }

        public static float2 ToFloat2(this Vector3 self)
        {
            return new float2(self.x, self.y);
        }
    }

    public static class Float3Extensions
    {
        public static Vector3 ToVector3(this float3 self)
        {
            return new Vector3(self.x, self.y, self.z);
        }

        public static Vector2 ToVector2(this float3 self)
        {
            return new Vector2(self.x, self.y);
        }
    }
}