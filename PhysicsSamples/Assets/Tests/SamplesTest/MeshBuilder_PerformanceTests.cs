using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Unity.Physics.Tests.Collision.Colliders
{
    class MeshBuilder_PerformanceTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Execute dummy job just to get Burst compilation out of the way.
            using (var dummyVertices = new NativeArray<float3>(1, Allocator.TempJob))
            using (var dummyTriangles = new NativeArray<int3>(1, Allocator.TempJob))
            {
                new TestMeshBuilderJob
                {
                    DummyRun = true,
                    Vertices = dummyVertices,
                    Triangles = dummyTriangles
                }.Run();
            }

            // collect the inputs to use from a dense mesh asset
            var mesh = Resources.Load<UnityEngine.Mesh>("VolcanicTerrain_80000");

            m_Vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent).Reinterpret<float3>();

            var indices = new List<int>();
            var allIndices = new List<int>();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; ++subMesh)
            {
                mesh.GetIndices(indices, subMesh);
                allIndices.AddRange(indices);
            }
            m_TriangleIndices = allIndices.ToNativeArray(Allocator.Persistent).Reinterpret<int3>(UnsafeUtility.SizeOf<int>());

            Assume.That(m_Vertices, Is.Not.Empty);
            Assume.That(m_TriangleIndices, Is.Not.Empty);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_Vertices.Dispose();
            m_TriangleIndices.Dispose();
        }

        NativeArray<float3> m_Vertices;
        NativeArray<int3> m_TriangleIndices;

        /// <summary>
        /// Measure performance of creation of <see cref="MeshCollider"/>.
        /// </summary>
        [Test, Performance]
        [TestCase(TestName = "MeshBuilderPerfTest")]
        public void MeshBuilderPerfTest()
        {
            var job = new TestMeshBuilderJob
            {
                DummyRun = false,
                Vertices = m_Vertices,
                Triangles = m_TriangleIndices
            };
            Measure.Method(() => job.Run()).MeasurementCount(1).Run();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestMeshBuilderJob : IJob
        {
            public bool DummyRun;

            public NativeArray<float3> Vertices;
            public NativeArray<int3> Triangles;

            public void Execute()
            {
                if (DummyRun)
                    return;

                MeshCollider.Create(Vertices, Triangles);
            }
        }
    }
}
