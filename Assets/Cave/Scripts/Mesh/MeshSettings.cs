using UnityEngine;

[CreateAssetMenu(fileName = "MeshSettings", menuName = "ScriptableObjects/MeshSettings", order = 2)]
public class MeshSettings : ScriptableObject
{
    public enum Generator
    {
        Cube,
        Tetrahedron
    }
    public Generator GeneratorType = Generator.Tetrahedron;
    public bool WeldVertices = true;
    public float SurfaceCutOff = 0f;
}
