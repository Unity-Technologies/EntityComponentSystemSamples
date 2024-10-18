using System;
using NUnit.Framework;
using Unity.Physics.Authoring;
using Unity.Physics.Editor;
using Unity.Physics.Tests.Authoring;

namespace Unity.Physics.Tests.Editor
{
    class StatusMessageUtility_IntegrationTests : BaseHierarchyConversionTest
    {
        [Test]
        public void GetHierarchyStatusMessage_WhenRoot_MessageNullOrEmpty()
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), Array.Empty<Type>());

            StatusMessageUtility.GetHierarchyStatusMessage(new[] { Root.transform }, out var msg);

            Assert.That(msg, Is.Null.Or.Empty);
        }

        [Test]
        public void GetHierarchyStatusMessage_WhenChild_AndChildIsNotPrimaryBody_MessageNullOrEmpty(
            [Values(
                typeof(UnityEngine.Rigidbody), typeof(UnityEngine.BoxCollider),
                typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)
             )]
            Type parentComponentType,
            [Values(
                typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.MeshCollider), typeof(UnityEngine.SphereCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type childComponentType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), new[] { parentComponentType }, new[] { childComponentType });
            Assume.That(PhysicsShapeExtensions.GetPrimaryBody(Child), Is.EqualTo(Parent));

            StatusMessageUtility.GetHierarchyStatusMessage(new[] { Child.GetComponent(childComponentType) }, out var msg);

            Assert.That(msg, Is.Null.Or.Empty);
        }

        [Test]
        public void GetHierarchyStatusMessage_WhenChild_AndChildIsPrimaryBody_MessageNotNullOrEmpty(
            [Values(
                typeof(UnityEngine.Rigidbody), typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.MeshCollider), typeof(UnityEngine.SphereCollider),
                typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring)
             )]
            Type childComponentType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { childComponentType });
            Assume.That(PhysicsShapeExtensions.GetPrimaryBody(Child), Is.EqualTo(Child));

            StatusMessageUtility.GetHierarchyStatusMessage(new[] { Child.GetComponent(childComponentType) }, out var msg);

            Assert.That(msg, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void GetHierarchyStatusMessage_WhenChild_AndChildHasBodyAndShape_QueryingBodyReturnsMessage(
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type bodyType,
            [Values(
                typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.MeshCollider), typeof(UnityEngine.SphereCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type shapeType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { bodyType, shapeType });
            Assume.That(PhysicsShapeExtensions.GetPrimaryBody(Child), Is.EqualTo(Child));

            StatusMessageUtility.GetHierarchyStatusMessage(new[] { Child.GetComponent(bodyType) }, out var msg);

            Assert.That(msg, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void GetHierarchyStatusMessage_WhenChild_AndChildHasBodyAndShape_QueryingShapeReturnsNullOrEmpty(
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type bodyType,
            [Values(
                typeof(UnityEngine.BoxCollider), typeof(UnityEngine.CapsuleCollider), typeof(UnityEngine.MeshCollider), typeof(UnityEngine.SphereCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type shapeType
        )
        {
            CreateHierarchy(Array.Empty<Type>(), Array.Empty<Type>(), new[] { bodyType, shapeType });
            Assume.That(PhysicsShapeExtensions.GetPrimaryBody(Child), Is.EqualTo(Child));

            StatusMessageUtility.GetHierarchyStatusMessage(new[] { Child.GetComponent(shapeType) }, out var msg);

            Assert.That(msg, Is.Null.Or.Empty);
        }
    }
}
