using System;
using Unity.Collections;

// Adapted from https://github.com/Scrawk/Marching-Cubes
// https://github.com/Scrawk/Marching-Cubes/blob/master/LICENSE

public abstract class MeshLookup : IDisposable
{
    public static MeshLookup GetInstance(MeshSettings settings = null)
    {
        if (instance != null)
        {
            return instance;
        }

        switch (settings.GeneratorType)
        {
            case MeshSettings.Generator.Tetrahedron:
                instance = new MeshLookupTetrahedron();
                break;

            case MeshSettings.Generator.Cube:
            default:
                instance = new MeshLookupCube();
                break;
        }

        return instance;
    }

    private static MeshLookup instance;

    public NativeArray<int> VertexOffsets { get; protected set; }
    public NativeArray<int> VertexIndices { get; protected set; }
    public NativeArray<int> CubeVertexIndices { get; protected set; }
    public NativeArray<int> EdgeDirections { get; protected set; }
    public NativeArray<int> EdgeFlags { get; protected set; }
    public NativeArray<int> Triangles { get; protected set; }

    public MeshLookup()
    {
        VertexOffsets = new NativeArray<int>(
            new int[]
            {
                0, 0, 0,
                1, 0, 0,
                1, 1, 0,
                0, 1, 0,
                0, 0, 1,
                1, 0, 1,
                1, 1, 1,
                0, 1, 1
            },
            Allocator.Persistent
        );
    }

    public virtual void Dispose()
    {
        VertexOffsets.Dispose();
        VertexIndices.Dispose();
        EdgeFlags.Dispose();
        Triangles.Dispose();
    }
}
