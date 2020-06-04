using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

// Adapted from https://github.com/Scrawk/Marching-Cubes
// https://github.com/Scrawk/Marching-Cubes/blob/master/LICENSE

public class MeshGeneratorTetrahedron : MeshGenerator
{
    [BurstCompile]
    private struct GenerateMesh : IJob
    {
        [WriteOnly]
        public NativeList<int> indexBuffer;
        [WriteOnly]
        public NativeList<Vector3> vertexBuffer;

        [ReadOnly]
        public float surfaceCutOff;
        [ReadOnly]
        public NativeArray<int> vertexOffsets;
        [ReadOnly]
        public NativeArray<int> edgeVertexIndices;
        [ReadOnly]
        public NativeArray<int> cubeVertexIndices;
        [ReadOnly]
        public NativeArray<int> edgeFlags;
        [ReadOnly]
        public NativeArray<int> triangles;

        [ReadOnly]
        public Vector3Int size;
        [ReadOnly]
        public Vector3Int paddedSize;
        [ReadOnly]
        public NativeArray<float> voxels;

        // read/write
        public NativeArray<float3> edgeVertices;
        // read/write
        public NativeArray<float> cubeVoxels;
        // read/write
        public NativeArray<float3> cubePositions;
        // read/write
        public NativeArray<float3> tetrahedronPositions;
        // read/write
        public NativeArray<float> tetrahedronValues;

        [ReadOnly]
        public bool weld;
        // read/write
        public NativeHashMap<float3, int> weldMap;

        public void Execute()
        {
            int xy = paddedSize.x * paddedSize.y;
            
            for (int n = 0, x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int i3 = i * 3;
                            int ix = x + vertexOffsets[i3];
                            int iy = y + vertexOffsets[i3 + 1];
                            int iz = z + vertexOffsets[i3 + 2];
                            cubeVoxels[i] = voxels[ix + iy * paddedSize.x + iz * xy];
                            cubePositions[i] = new float3(
                                x + vertexOffsets[i3],
                                y + vertexOffsets[i3 + 1],
                                z + vertexOffsets[i3 + 2]
                            );
                        }

                        for (int i = 0; i < 6; i++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                int vi = cubeVertexIndices[i * 4 + j];
                                tetrahedronPositions[j] = cubePositions[vi];
                                tetrahedronValues[j] = cubeVoxels[vi];
                            }

                            int flagIndex = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                if (tetrahedronValues[j] <= surfaceCutOff)
                                {
                                    flagIndex |= 1 << j;
                                }
                            }

                            int edgeFlag = edgeFlags[flagIndex]; 
                            if (!edgeFlag.Equals(0))
                            {
                                for (int j = 0; j < 6; j++)
                                {
                                    if (!(edgeFlag & (1 << j)).Equals(0))
                                    {
                                        int vi0 = edgeVertexIndices[j * 2];
                                        int vi1 = edgeVertexIndices[j * 2 + 1];
                                        float delta = tetrahedronValues[vi1] - tetrahedronValues[vi0];
                                        float offset = delta.Equals(0f) ? surfaceCutOff : (surfaceCutOff - tetrahedronValues[vi0]) / delta;
                                        edgeVertices[j] = (1 - offset) * tetrahedronPositions[vi0] + offset * tetrahedronPositions[vi1];
                                    }
                                }

                                for (int j = 0; j < 2; j++)
                                {
                                    if (triangles[flagIndex * 7 + 3 * j] < 0) break;

                                    for (int k = 0; k < 3; k++)
                                    {
                                        int v = triangles[flagIndex * 7 + 3 * j + k];
                                        if (weld)
                                        {
                                            if (weldMap.TryGetValue(edgeVertices[v], out int index))
                                            {
                                                indexBuffer.Add(index);
                                            }
                                            else
                                            {
                                                weldMap.TryAdd(edgeVertices[v], n);
                                                vertexBuffer.Add(edgeVertices[v]);
                                                indexBuffer.Add(n);
                                                n++;
                                            }
                                        }
                                        else
                                        {
                                            vertexBuffer.Add(edgeVertices[v]);
                                            indexBuffer.Add(n);
                                            n++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private GenerateMesh job;

    public MeshGeneratorTetrahedron(MeshSettings settings) : base(settings)
    {
    }

    public override void Schedule()
    {
        base.Schedule();

        job = new GenerateMesh()
        {
            indexBuffer = indexBuffer,
            vertexBuffer = vertexBuffer,
            surfaceCutOff = settings.SurfaceCutOff,
            weld = settings.WeldVertices,
            weldMap = weldMap,

            vertexOffsets = lookup.VertexOffsets,
            edgeVertexIndices = lookup.VertexIndices,
            cubeVertexIndices = lookup.CubeVertexIndices,
            edgeFlags = lookup.EdgeFlags,
            triangles = lookup.Triangles,

            voxels = VoxelGenerator.Volume.Voxels,
            size = VoxelGenerator.Volume.Size,
            paddedSize = VoxelGenerator.Volume.PaddedSize,
            
            edgeVertices = new NativeArray<float3>(6, Allocator.TempJob),
            cubeVoxels = new NativeArray<float>(8, Allocator.TempJob),
            cubePositions = new NativeArray<float3>(8, Allocator.TempJob),
            tetrahedronPositions = new NativeArray<float3>(4, Allocator.TempJob),
            tetrahedronValues = new NativeArray<float>(4, Allocator.TempJob)
        };

        jobHandle = job.Schedule();
    }

    public override void Complete(bool updateMesh = true)
    {
        base.Complete(updateMesh);
        job.edgeVertices.Dispose();
        job.cubeVoxels.Dispose();
        job.cubePositions.Dispose();
        job.tetrahedronPositions.Dispose();
        job.tetrahedronValues.Dispose();
    }
}


