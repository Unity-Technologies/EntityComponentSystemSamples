#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // Client-side aiming arrow for the drag shot, drawn with line renderers: a shaft and a head at the tip, plus a
    // faded shaft for the part of the drag beyond the speed cap.
    public class ShotAimArrow2D : MonoBehaviour
    {
        const float k_Thickness = 0.15f;
        const float k_HeadLength = 0.8f;
        const float k_HeadAngle = 30f;
        const float k_BeyondMaxOpacity = 0.7f;
        const int k_SortingOrder = 100;
        static readonly Color k_Color = new Color(1f, 0.85f, 0.2f);

        LineRenderer m_Shaft;
        LineRenderer m_BeyondMaxShaft;
        LineRenderer m_Head;

        public static ShotAimArrow2D Create()
        {
            var arrow = new GameObject("Shot aim arrow").AddComponent<ShotAimArrow2D>();
            var material = new Material(Shader.Find("Sprites/Default"));
            arrow.m_Shaft = arrow.CreateLine("Shaft", material, 1f, 2);
            arrow.m_BeyondMaxShaft = arrow.CreateLine("Shaft beyond max", material, k_BeyondMaxOpacity, 2);
            arrow.m_Head = arrow.CreateLine("Head", material, 1f, 3);
            arrow.gameObject.SetActive(false);
            return arrow;
        }

        LineRenderer CreateLine(string lineName, Material material, float opacity, int pointCount)
        {
            var line = new GameObject(lineName).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.positionCount = pointCount;
            line.widthMultiplier = k_Thickness;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            var color = k_Color;
            color.a = opacity;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = k_SortingOrder;
            return line;
        }

        // Draws from `tail` to the tip at `tip`. The `maxLength` nearest the tip is solid; anything beyond is faded.
        public void Show(Vector2 tail, Vector2 tip, float maxLength)
        {
            var vector = tip - tail;
            var length = vector.magnitude;
            if (length < Mathf.Epsilon)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            var direction = vector / length;
            var solidStart = tip - direction * Mathf.Min(length, maxLength);
            m_Shaft.SetPosition(0, solidStart);
            m_Shaft.SetPosition(1, tip);

            var beyondMax = length > maxLength;
            m_BeyondMaxShaft.gameObject.SetActive(beyondMax);
            if (beyondMax)
            {
                m_BeyondMaxShaft.SetPosition(0, tail);
                m_BeyondMaxShaft.SetPosition(1, solidStart);
            }

            var back = -direction * Mathf.Min(k_HeadLength, length * 0.5f);
            m_Head.SetPosition(0, tip + (Vector2)(Quaternion.Euler(0f, 0f, k_HeadAngle) * back));
            m_Head.SetPosition(1, tip);
            m_Head.SetPosition(2, tip + (Vector2)(Quaternion.Euler(0f, 0f, -k_HeadAngle) * back));
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
#endif
