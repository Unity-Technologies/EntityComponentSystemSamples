using UnityEditor;

#if UNITY_EDITOR

namespace Unity.Physics.Editor
{
    static class CustomComponentVersionUpgrader
    {
        [MenuItem("Tools/Unity Physics/Upgrade Physics Shape Versions")]
        static void UpgradeAssetsWithPhysicsShape()
        {
            ComponentVersionUpgrader.UpgradeAssets<Authoring.PhysicsShapeAuthoring>("Physics Shape");
        }
    }
}

#endif
