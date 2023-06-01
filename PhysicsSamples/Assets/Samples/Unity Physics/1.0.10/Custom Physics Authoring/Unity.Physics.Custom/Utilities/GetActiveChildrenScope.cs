using System;
using System.Collections.Generic;
using UnityEngine;
using UnityComponent = UnityEngine.Component;

namespace Unity.Physics.Authoring
{
    public struct GetActiveChildrenScope<T> : IDisposable where T : UnityComponent
    {
        static readonly List<PhysicsShapeAuthoring> s_PhysicsShapes = new List<PhysicsShapeAuthoring>(8);

        static bool s_BufferUsed;
        static List<T> s_Buffer = new List<T>(8);

        public List<T> Buffer => m_Disposed ? null : s_Buffer;

        bool m_Disposed;
        PhysicsShapeAuthoring m_Shape;
        Transform m_Root;
        GameObject m_PrimaryBody;
        bool m_CheckIfComponentBelongsToShape;

        public GetActiveChildrenScope(PhysicsShapeAuthoring shape, Transform root)
        {
            m_Disposed = false;
            m_Shape = shape;
            m_Root = root;
            m_PrimaryBody = PhysicsShapeExtensions.GetPrimaryBody(root.gameObject);
            m_CheckIfComponentBelongsToShape = root.transform.IsChildOf(shape.transform);
            if (s_BufferUsed)
                throw new InvalidOperationException($"Cannot nest two {GetType()}");
            s_BufferUsed = true;
            root.GetComponentsInChildren(true, s_Buffer);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            s_BufferUsed = false;
            s_Buffer.Clear();
        }

        public bool IsChildActiveAndBelongsToShape(T child, bool filterOutInvalid = true)
        {
            var meshFilter = (UnityComponent)child as MeshFilter;
            if (meshFilter != null)
            {
                if (meshFilter.sharedMesh == null)
                    return false;

                var renderer = meshFilter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled)
                    return false;

                if (filterOutInvalid && !meshFilter.sharedMesh.IsValidForConversion(m_Shape.gameObject))
                    return false;
            }

            if (m_CheckIfComponentBelongsToShape)
            {
                if (PhysicsShapeExtensions.GetPrimaryBody(child.gameObject) != m_PrimaryBody)
                    return false;

                child.gameObject.GetComponentsInParent(true, s_PhysicsShapes);
                if (s_PhysicsShapes[0] != m_Shape)
                {
                    s_PhysicsShapes.Clear();
                    return false;
                }
            }

            // do not simply use GameObject.activeInHierarchy because it will be false when instantiating a prefab
            var t = child.transform;
            var activeInHierarchy = t.gameObject.activeSelf;
            while (activeInHierarchy && t != m_Root)
            {
                t = t.parent;
                activeInHierarchy &= t.gameObject.activeSelf;
            }

            return activeInHierarchy;
        }
    }
}
