using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace HelloCube.MySample._01
{
    /// <summary>
    /// UnityのMonoBehaviourのイベント(Update, Start, Awake)のように
    /// Entityでもサポートされているイベントが存在する.
    ///
    /// 仮想クラスであるComponentSystemを継承して実装する
    /// 最低限OnUpdateメソッドは実装する必要がある
    /// </summary>
    public class MoveToSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref MoveToComponent moveTo, ref Translation translation) =>
            {
                // 更新時間によって変化をつける場合は、Time.deltaTimeを利用.
                var deltaTime = Time.deltaTime;

                // 現在位置の取得. (float3のSmoothメソッドがないので、Vector3に変換)
                var pos = translation.Value.ToVector3();

                // SmoothDampによる移動処理
                // TODO Simulator.DeltaTimeを使用すること
                var newPos = Vector3.SmoothDamp(pos, moveTo.to, ref moveTo.velocity, moveTo.smoothTime);

                // 位置のセット. (Vector3からfloat3に変換)
                translation.Value = newPos.ToFloat3();
            });
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