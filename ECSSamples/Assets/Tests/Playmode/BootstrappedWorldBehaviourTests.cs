using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace BootstrappedWorldTests
{
    [TestFixture]
    public class BootstrappedWorldBehaviourTests
    {
        /// <summary>
        /// If this test fails, either no systems were added by default (which means that something failed in
        /// bootstrapping or system discovery), or an unexpected system was added that creates entities.  This usually
        /// means a sample/demo system is running by default.
        ///
        /// There is an expected number of entities after one frame, as some built-in systems (such as Time) create
        /// singleton entities for tracking.
        /// </summary>
        ///
        [UnityTest]
        public IEnumerator DefaultBootstrappedWorldWithAllSystems_WaitingOneFrame_AsExpected()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            const int ExpectedEntityCount = 1;

            yield return new WaitForFixedUpdate();

            Assert.Greater(world.Systems.Count, 0, "No systems were found; bootstrapping or system discovery failed");
            Assert.AreEqual(ExpectedEntityCount, GetEntityCount(world), GetTruncatedEntitiesDescription(world, 50));
        }

        static int GetEntityCount(World world)
        {
            using (var entities = world.EntityManager.GetAllEntities())
            {
                return entities.Length;
            }
        }

        static string GetTruncatedEntitiesDescription(World world, int truncateAfter)
        {
            var builder = new StringBuilder();
            var lines = 0;

            string Truncate(StringBuilder sb)
            {
                sb.AppendLine("Truncating further output...");
                return sb.ToString();
            }

            using (var entities = world.EntityManager.GetAllEntities())
            {
                foreach (var entity in entities)
                {
                    builder.AppendLine(entity + " with components:");
                    if (++lines >= truncateAfter) return Truncate(builder);

                    using (var componentTypes = world.EntityManager.GetComponentTypes(entity))
                    {
                        foreach (var componentType in componentTypes)
                        {
                            builder.AppendLine("  - " + TypeManager.GetType(componentType.TypeIndex));
                            if (++lines >= truncateAfter) return Truncate(builder);
                        }
                    }
                }
            }

            return builder.ToString();
        }
    }
}
