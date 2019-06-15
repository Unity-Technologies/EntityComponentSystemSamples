using Unity.Entities;
using UnityEngine;
 
namespace HelloCube.MySample._02
{
    /// <summary>
    /// MySample01のジョブシステム版
    /// </summary>
    [RequiresEntityConversion]
    public class MoveToAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float speed;
        public Vector3 to;
        public Vector3 velocity;
        public float smoothTime;
 
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // ここでデータをnewする.
            var data = new MoveTo {speed = speed, to = to, velocity = velocity, smoothTime = smoothTime};
 
            // エンティティにComponentを登録.
            // dataはIComponentDataインターフェースを継承、かつStruct(構造体)でないとダメ.
            // AddComponentに近いことを行うが、内部的にはC#のメモリ管理システムは使わずに
            // unsafeを使ったポインタによる、メモリ管理を行っている.
            dstManager.AddComponentData(entity, data);
        }
    }
}