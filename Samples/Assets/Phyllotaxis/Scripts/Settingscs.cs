using Unity.Rendering;
using UnityEngine;

public class Settingscs : MonoBehaviour
{
    public Mesh cube;
    public float lerpFact = 1;
    public Material mat;
    private MeshInstanceRenderer MSI;
    public int nbOfCubes = 20;
    public float radius = 2;

    private void Start()
    {
        var GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = GO.GetComponent<MeshFilter>().mesh;

        MSI = new MeshInstanceRenderer();
        MSI.material = mat;
        MSI.mesh = cube;
        Destroy(GO);
    }

    public MeshInstanceRenderer getMSI()
    {
        return MSI;
    }
}
