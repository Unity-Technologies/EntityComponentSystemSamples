using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.MySample._01b
{
    public class PrefabSpawnBehaviour : MonoBehaviour
    {
        public GameObject Prefab;
        public int CountX = 100;
        public int CountY = 100;

        void Start()
        {
            // Entity Prefabをヒエラルキーから一度だけ生成
            Entity prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Prefab, World.Active);
            var entityManager = World.Active.EntityManager;

            for (int x = 0; x < CountX; x++)
            {
                for (int y = 0; y < CountY; y++)
                {
                    // Entity化されたprefabをインスタンス化(通常のInstantiateよりコスト安)
                    var instance = entityManager.Instantiate(prefab);
                    
                    // インスタンス化されたEntityをパーリンノイズで用いたグリッド上に配置
                    var position = transform.TransformPoint(new float3(x - CountX / 2,
                        noise.cnoise(new float2(x, y) * 0.21F) * 10, y - CountY / 2));

                    // 各種コンポーネントの初期化と登録
                    entityManager.SetComponentData(instance, new Translation() {Value = position});
                    entityManager.AddComponentData(instance, new MoveUp());
                    entityManager.AddComponentData(instance, new MovingCube());
                }
            }
        }
    }
}