using System;
using Unity.Entities;
using UnityEngine;

namespace HelloCube.MySample._01
{
    /// <summary>
    /// Tips:
    ///  IComponentDataを継承させるComponentはstructである必要がある.
    ///  Serializableは、インスペクタチェック用?
    /// </summary>
    [Serializable]
    public struct MoveToComponent : IComponentData
    {
        public float speed;
        public Vector3 to;
        public Vector3 velocity;
        public float smoothTime;
    }
}