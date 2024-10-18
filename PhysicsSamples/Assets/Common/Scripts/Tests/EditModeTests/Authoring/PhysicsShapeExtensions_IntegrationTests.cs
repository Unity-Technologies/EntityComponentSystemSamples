using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Physics.Authoring;

namespace Unity.Physics.Tests.Authoring
{
    class PhysicsShapeExtensions_IntegrationTests : BaseHierarchyConversionTest
    {
        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsMultipleBodies_ReturnsFirstParent(
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type rootBodyType,
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type parentBodyType
        )
        {
            CreateHierarchy(new[] { rootBodyType }, new[] { parentBodyType }, Array.Empty<Type>());

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Parent));
        }

        [Test]
        public void GetPrimaryBody_WhenFirstParentPhysicsBodyIsDisabled_ReturnsFirstEnabledAncestor(
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type rootBodyType,
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type parentBodyType
        )
        {
            CreateHierarchy(new[] { rootBodyType }, new[] { parentBodyType }, new[] { typeof(PhysicsBodyAuthoring) });
            // if root is PhysicsBodyAuthoring, test assumes it is enabled; Rigidbody is Component and cannot be disabled
            Assume.That(Root.GetComponent<PhysicsBodyAuthoring>()?.enabled ?? true, Is.True);
            Child.GetComponent<PhysicsBodyAuthoring>().enabled = false;

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Parent));
        }

        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsBody_AndIsStaticOptimized_ReturnsBody(
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type parentBodyType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type childShapeType
        )
        {
            CreateHierarchy(new[] { typeof(StaticOptimizeEntity) }, new[] { parentBodyType }, new[] { childShapeType });

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Parent));
        }

        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsBody_AndIsStatic_ReturnsBody(
            [Values(
                typeof(UnityEngine.Rigidbody),
                typeof(PhysicsBodyAuthoring)
             )]
            Type parentBodyType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type childShapeType
        )
        {
            CreateHierarchy(true, Array.Empty<Type>(), new[] { parentBodyType }, new[] { childShapeType });

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Parent));
        }

        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsNoBodies_ReturnsTopMostShape(
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type rootShapeType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type parentShapeType,
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type childShapeType
        )
        {
            CreateHierarchy(new[] { rootShapeType }, new[] { parentShapeType }, new[] { childShapeType });

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Root));
        }

        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsNoBodies_IsStaticOptimized_ReturnsStaticOptimizeEntity(
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type childShapeType
        )
        {
            CreateHierarchy(new[] { typeof(StaticOptimizeEntity) }, Array.Empty<Type>(), new[] { childShapeType });

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Root));
        }

        [Test]
        public void GetPrimaryBody_WhenHierarchyContainsNoBodies_IsStatic_ReturnsStaticOptimizeEntity(
            [Values(
                typeof(UnityEngine.BoxCollider),
                typeof(PhysicsShapeAuthoring)
             )]
            Type childShapeType
        )
        {
            CreateHierarchy(true, Array.Empty<Type>(), Array.Empty<Type>(), new[] { childShapeType });

            var primaryBody = PhysicsShapeExtensions.GetPrimaryBody(Child);

            Assert.That(primaryBody, Is.EqualTo(Root));
        }
    }
}
