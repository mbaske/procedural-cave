using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

// Adapted from https://github.com/Scrawk/Marching-Cubes
// https://github.com/Scrawk/Marching-Cubes/blob/master/LICENSE

public class MeshGeneratorCube : MeshGenerator
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
        public NativeArray<int> edgeDirections;
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
                        int flagIndex = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            int i3 = i * 3;
                            int ix = x + vertexOffsets[i3];
                            int iy = y + vertexOffsets[i3 + 1];
                            int iz = z + vertexOffsets[i3 + 2];
                            cubeVoxels[i] = voxels[ix + iy * paddedSize.x + iz * xy];
                            
                            if (cubeVoxels[i] <= surfaceCutOff)
                            {
                                flagIndex |= 1 << i;
                            }
                        }

                        int edgeFlag = edgeFlags[flagIndex];
                        if (!edgeFlag.Equals(0))
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                if (!(edgeFlag & (1 << i)).Equals(0))
                                {
                                    int evi = edgeVertexIndices[i * 2];
                                    int evi3 = evi * 3;
                                    int i3 = i * 3;
                                    float delta = cubeVoxels[edgeVertexIndices[i * 2 + 1]] - cubeVoxels[evi];
                                    float offset = delta.Equals(0f) ? surfaceCutOff : (surfaceCutOff - cubeVoxels[evi]) / delta;
                                    edgeVertices[i] = new float3(
                                        x + (vertexOffsets[evi3] + offset * edgeDirections[i3]),
                                        y + (vertexOffsets[evi3 + 1] + offset * edgeDirections[i3 + 1]),
                                        z + (vertexOffsets[evi3 + 2] + offset * edgeDirections[i3 + 2])
                                    );
                                }
                            }

                            for (int i = 0; i < 5; i++)
                            {
                                if (triangles[flagIndex * 16 + 3 * i] < 0) break;

                                for (int j = 0; j < 3; j++)
                                {
                                    int v = triangles[flagIndex * 16 + 3 * i + j];
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

    private GenerateMesh job;

    public MeshGeneratorCube(MeshSettings settings) : base(settings)
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
            edgeDirections = lookup.EdgeDirections,
            edgeFlags = lookup.EdgeFlags,
            triangles = lookup.Triangles,

            voxels = VoxelGenerator.Volume.Voxels,
            size = VoxelGenerator.Volume.Size,
            paddedSize = VoxelGenerator.Volume.PaddedSize,
            
            edgeVertices = new NativeArray<float3>(12, Allocator.Persistent),
            cubeVoxels = new NativeArray<float>(8, Allocator.Persistent)
        };

        jobHandle = job.Schedule();
    }

    public override void Complete(bool updateMesh = true)
    {
        base.Complete(updateMesh);
        job.edgeVertices.Dispose();
        job.cubeVoxels.Dispose();
    }
}