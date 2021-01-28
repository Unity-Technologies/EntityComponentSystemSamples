using UnityEngine;

[ExecuteInEditMode]
public class ColorizeMBP : MonoBehaviour
{
    public Color m_Color;

    void Update()
    {
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", m_Color);
        GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
    }
}
