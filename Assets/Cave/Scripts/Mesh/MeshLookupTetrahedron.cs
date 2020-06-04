using Unity.Collections;

// Adapted from https://github.com/Scrawk/Marching-Cubes
// https://github.com/Scrawk/Marching-Cubes/blob/master/LICENSE

public class MeshLookupTetrahedron : MeshLookup
{
    public MeshLookupTetrahedron()
    {
        VertexIndices = new NativeArray<int>(
            new int[]
            {
                0, 1,
                1, 2,
                2, 0,
                0, 3,
                1, 3,
                2, 3
            },
            Allocator.Persistent
        );

        CubeVertexIndices = new NativeArray<int>(
            new int[]
            {
                0, 5, 1, 6,
                0, 1, 2, 6,
                0, 2, 3, 6,
                0, 3, 7, 6,
                0, 7, 4, 6,
                0, 4, 5, 6
            },
            Allocator.Persistent
        );

        EdgeFlags = new NativeArray<int>(
            new int[]
            {
                0x00, 0x0d, 0x13, 0x1e, 0x26, 0x2b, 0x35, 0x38,
                0x38, 0x35, 0x2b, 0x26, 0x1e, 0x13, 0x0d, 0x00
            },
            Allocator.Persistent
        );

        Triangles = new NativeArray<int>(
            new int[]
            {
                -1, -1, -1, -1, -1, -1, -1,
                0, 3, 2, -1, -1, -1, -1,
                0, 1, 4, -1, -1, -1, -1,
                1, 4, 2, 2, 4, 3, -1,
                1, 2, 5, -1, -1, -1, -1,
                0, 3, 5, 0, 5, 1, -1,
                0, 2, 5, 0, 5, 4, -1,
                5, 4, 3, -1, -1, -1, -1,
                3, 4, 5, -1, -1, -1, -1,
                4, 5, 0, 5, 2, 0, -1,
                1, 5, 0, 5, 3, 0, -1,
                5, 2, 1, -1, -1, -1, -1,
                3, 4, 2, 2, 4, 1, -1,
                4, 1, 0, -1, -1, -1, -1,
                2, 3, 0, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1
            },
            Allocator.Persistent
        );
    }

    public override void Dispose()
    {
        base.Dispose();
        CubeVertexIndices.Dispose();
    }
}
