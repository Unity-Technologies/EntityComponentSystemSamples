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
        /// There is an expected number of entities after one frame, as some built-in systems (such as Time, and
        /// all the ECB systems) create singleton entities.
        /// </summary>
        ///
        [UnityTest]
        public IEnumerator DefaultBootstrappedWorldWithAllSystems_WaitingOneFrame_AsExpected()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            // In the Editor we bootstrap the GameObjectScene handle for the current scene when entering playmode, but not in a standalone player
            // Where we expect the users to control this
#if UNITY_EDITOR
            const int ExpectedEntityCount = 11;
#else
            const int ExpectedEntityCount = 10;
#endif

            yield return new WaitForFixedUpdate();

            Assert.Greater(world.Systems.Count, 0, "No systems were found; bootstrapping or system discovery failed");
            Assert.AreEqual(ExpectedEntityCount, GetEntityCountExceptSystems(world), GetTruncatedEntitiesDescription(world, 50));
        }

        static int GetEntityCountExceptSystems(World world)
        {
            return world.EntityManager.UniversalQuery.CalculateEntityCount();
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
