#if UNITY_EDITOR
using Unity.Entities;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class WireframeGizmo : MonoBehaviour
{
    static Material s_LineMaterial;
    static void CreateLineMaterial()
    {
        if (!s_LineMaterial)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            s_LineMaterial = new Material(shader);
            s_LineMaterial.hideFlags = HideFlags.HideAndDontSave;
            s_LineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_LineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_LineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            s_LineMaterial.SetInt("_ZWrite", 0);
        }
    }

    public void OnRenderObject()
    {
        CreateLineMaterial();
        s_LineMaterial.SetPass(0);

        DrawCircle(transform.position, Quaternion.LookRotation(transform.forward, transform.up));
        DrawCircle(transform.position, Quaternion.LookRotation(transform.up, transform.forward));
        DrawCircle(transform.position, Quaternion.LookRotation(Vector3.Cross(transform.up, transform.forward), transform.forward));
    }
    
    static void DrawCircle(Vector3 position, Quaternion rotation)
    {
        const float k_SphereSections = 32;
        const float k_Radius = 0.8f;
        
        float angle = 0.0f;
        Vector3 pt1 = Vector3.zero, pt2 = Vector3.zero;

        GL.Begin(GL.LINES);
        GL.Color(Color.white);
        for (int i = 0; i < k_SphereSections + 1; i++)
        {
            pt2.x = Mathf.Sin(angle) * k_Radius;
            pt2.y = Mathf.Cos(angle) * k_Radius;

            if (i > 0)
            {
                var transformedPt1 = rotation * pt1 + position;
                var transformedPt2 = rotation * pt2 + position;
                GL.Vertex3(transformedPt1.x, transformedPt1.y, transformedPt1.z);
                GL.Vertex3(transformedPt2.x, transformedPt2.y, transformedPt2.z);
            }

            pt1 = pt2;
            angle += Mathf.PI * 2.0f / k_SphereSections;
        }
        GL.End();
    }
}

[ConverterVersion("joe", 1)]
public class WireframeGizmoConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((WireframeGizmo wireframeGizmo) =>
        {
            AddHybridComponent(wireframeGizmo);
        });
    }
}

#endif // UNITY_EDITOR
