using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;

public static class MeshGeneratorFactory
{
    public static MeshGenerator CreateInstance(MeshSettings settings)
    {
        switch (settings.GeneratorType)
        {
            case MeshSettings.Generator.Tetrahedron:
                return new MeshGeneratorTetrahedron(settings);

            case MeshSettings.Generator.Cube:
            default:
                return new MeshGeneratorCube(settings);
        }
    }
}

public abstract class MeshGenerator : System.IDisposable
{
    public Mesh Mesh { get; private set; }
    public VoxelGenerator VoxelGenerator { protected get; set; }
    public bool IsCompleted => jobHandle.IsCompleted;

    protected NativeList<int> indexBuffer;
    protected NativeList<Vector3> vertexBuffer;
    protected NativeHashMap<float3, int> weldMap;
    protected MeshSettings settings;
    protected MeshLookup lookup;
    protected JobHandle jobHandle;

    public MeshGenerator(MeshSettings settings)
    {
        this.settings = settings;
        Mesh = new Mesh();
        lookup = MeshLookup.GetInstance(settings);
        Allocate();
    }

    protected virtual void Allocate()
    {
        // TODO Allocation size? Vertex & triangle count unknown prior to job execution.
        vertexBuffer = new NativeList<Vector3>(0, Allocator.Persistent);
        indexBuffer = new NativeList<int>(0, Allocator.Persistent);
        weldMap = new NativeHashMap<float3, int>(0, Allocator.Persistent);
    }

    public virtual void Schedule()
    {
        vertexBuffer.Clear();
        indexBuffer.Clear();
        weldMap.Clear();
    }

    public virtual void Complete(bool updateMesh = true)
    {
        jobHandle.Complete();

        if (updateMesh)
        {
            Mesh.Clear();
            Mesh.SetVertices(vertexBuffer.AsArray());
            Mesh.SetTriangles(indexBuffer.ToArray(), 0);
            Mesh.RecalculateBounds();
            Mesh.RecalculateNormals();
        }
    }

    public virtual void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        weldMap.Dispose();
    }
}