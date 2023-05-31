using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Extensions
{
    public unsafe class QueryTester : MonoBehaviour
    {
        public float Distance = 10.0f;
        public Vector3 Direction = new Vector3(1, 0, 0);
        public bool CollectAllHits = false;
        public bool DrawSurfaceNormal = true;
        public bool HighlightLeafCollider = true;
        public ColliderType ColliderType;
        public bool ColliderQuery;
        [Tooltip("Applied only if ColliderQuery == true")]
        public float InputColliderScale = 1.0f;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Vector3 dir = (transform.rotation * Direction) * Distance;
            Gizmos.DrawRay(transform.position, dir);
        }
    }

    public class QueryData : IComponentData
    {
        // authoring data
        public float Distance;
        public float3 Direction;
        public bool CollectAllHits;
        public bool DrawSurfaceNormal;
        public bool HighlightLeafCollider;
        public bool ColliderQuery;
        public ColliderType ColliderType;
        public float InputColliderScale;

        // calculated data
        public bool ColliderDataInitialized;
        public BlobAssetReference<Collider> Collider;
        public BlobAssetReference<Collider>[] ChildrenColliders;
        public UnityEngine.Mesh[] ColliderMeshes;
    }

    class QueryTesterBaker : Baker<QueryTester>
    {
        public override void Bake(QueryTester authoring)
        {
            QueryData queryData = new QueryData();
            queryData.Distance = authoring.Distance;
            queryData.Direction = authoring.Direction;
            queryData.CollectAllHits = authoring.CollectAllHits;
            queryData.DrawSurfaceNormal = authoring.DrawSurfaceNormal;
            queryData.HighlightLeafCollider = authoring.HighlightLeafCollider;
            queryData.ColliderQuery = authoring.ColliderQuery;
            queryData.ColliderType = authoring.ColliderType;
            queryData.InputColliderScale = authoring.InputColliderScale;
            queryData.ColliderDataInitialized = false;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, queryData);
        }
    }
}
