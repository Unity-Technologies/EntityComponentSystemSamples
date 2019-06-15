using Unity.Entities;
 using UnityEngine;
 
 namespace HelloCube.MySample._01
 {
     /// <summary>
     /// "Authoring = 編集支援"
     /// このクラスの役割は、MonoBehaviour上のデータを
     /// ComponentDataとしてEntityへ追加すること
     /// 
     ///   -> Inspector操作によるサポートが目的
     ///   -> つまりデータソースをインスペクタではなくcsvにする場合は不要
     /// 
     /// ちなみに、このUnity社ではコードのcommitterは
     /// 'GameCode CI'なので自動生成コードと思われる
     ///
     /// また、このScriptはConvertToEntityというScriptとセットで使用する
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
             var data = new MoveToComponent {speed = speed, to = to, velocity = velocity, smoothTime = smoothTime};
 
             // エンティティにComponentを登録.
             // dataはIComponentDataインターフェースを継承、かつStruct(構造体)でないとダメ.
             // AddComponentに近いことを行うが、内部的にはC#のメモリ管理システムは使わずに
             // unsafeを使ったポインタによる、メモリ管理を行っている.
             dstManager.AddComponentData(entity, data);
         }
     }
 }