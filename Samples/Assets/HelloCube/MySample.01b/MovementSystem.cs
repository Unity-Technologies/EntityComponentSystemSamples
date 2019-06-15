using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace HelloCube.MySample._01b
{
    // シーンの全Entityの位置を更新します
    // MoveUpコンポーネントを持っているかいないかに依存して振る舞いが変わります
    public class MovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // MoveUpが存在する場合の更新処理
            Entities.WithAllReadOnly<MovingCube, MoveUp>().ForEach((Entity id, ref Translation translation) =>
            {
                var deltaTime = Time.deltaTime;
                
                // 経過時間に応じて上に移動
                translation = new Translation()
                {
                    Value = new float3(translation.Value.x, translation.Value.y + deltaTime, translation.Value.z)
                };

                // 閾値より上に到達した場合、MoveUpコンポーネントを削除(状態の変化)
                if (translation.Value.y > 10.0f)
                {
                    // Entityを指定して削除を行う
                    PostUpdateCommands.RemoveComponent<MoveUp>(id);
                }
            });

            // MoveUpが存在しない場合の更新処理
            Entities.WithAllReadOnly<MovingCube>().WithNone<MoveUp>().ForEach((Entity id, ref Translation translation) =>
            {
                // 一気に画面外まで移動させる
                translation = new Translation()
                {
                    Value = new float3(translation.Value.x, -10.0f, translation.Value.z)
                };
                
                // 再びMoveUpコンポーネントを追加(状態の変化)
                PostUpdateCommands.AddComponent(id, new MoveUp());
            });
        }
    }
}