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
        /// If this test fails a System was added which spawns entities by default.
        /// </summary>
        ///
        [UnityTest]
        public IEnumerator DefaultBootstrappedWorld_AddingAllSystemsAndWaitingOneFrame_NoEntitiesAreCreated()
        {
            var world = World.Active;
            Assert.Greater(world.Systems.Count(), 0);

            yield return new WaitForFixedUpdate();

            Assert.Zero(GetEntityCount(world), GetTruncatedEntitiesDescription(world, 50));
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
