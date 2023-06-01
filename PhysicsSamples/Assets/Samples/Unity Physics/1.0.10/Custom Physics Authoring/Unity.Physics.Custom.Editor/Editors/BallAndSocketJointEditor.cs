#if UNITY_EDITOR

using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(BallAndSocketJoint))]
    public class BallAndSocketEditor : UnityEditor.Editor
    {
        protected virtual void OnSceneGUI()
        {
            BallAndSocketJoint ballAndSocket = (BallAndSocketJoint)target;

            EditorGUI.BeginChangeCheck();

            EditorUtilities.EditPivot(ballAndSocket.worldFromA, ballAndSocket.worldFromB, ballAndSocket.AutoSetConnected,
                ref ballAndSocket.PositionLocal, ref ballAndSocket.PositionInConnectedEntity, ballAndSocket);
        }
    }
}

#endif
