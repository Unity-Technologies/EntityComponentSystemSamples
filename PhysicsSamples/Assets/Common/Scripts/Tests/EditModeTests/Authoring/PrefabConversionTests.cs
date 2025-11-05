using NUnit.Framework;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Unity.Physics.Tests.Authoring
{
    class PrefabConversionTestsCustom : PrefabConversionTestsBase
    {
        [Ignore("Failing in Packageworks, https://jira.unity3d.com/browse/DOTS-10120")]
        [Test]
        public void PrefabConversionCustom_ChildCollider_ForceUnique([Values] bool forceUniqueCollider)
        {
            var rigidBody = new GameObject("Parent", new[] { typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)});

            rigidBody.GetComponent<PhysicsShapeAuthoring>().SetBox(new BoxGeometry(), EulerAngles.Default);
            rigidBody.GetComponent<PhysicsBodyAuthoring>().MotionType = BodyMotionType.Dynamic;

            ValidatePrefabChildColliderUniqueStatus(rigidBody, forceUniqueCollider,
                (gameObject, mass) =>
                {
                    gameObject.GetComponent<PhysicsBodyAuthoring>().Mass = mass;
                },
                (gameObject) =>
                {
                    gameObject.GetComponent<PhysicsShapeAuthoring>().ForceUnique = true;
                });
        }
    }
}
