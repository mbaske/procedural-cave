using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

public class VoxelGeneratorNoise : VoxelGenerator
{
    [BurstCompile]
    private struct GenerateVoxels : IJobParallelFor
    {
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float> voxels;

        [ReadOnly]
        public Vector3Int paddedSize;
        [ReadOnly]
        public float3 position;
        [ReadOnly]
        public float3 scale;
        [ReadOnly]
        public float3 offset;

        public void Execute(int x)
        {
            int xy = paddedSize.x * paddedSize.y;

            for (int y = 0; y < paddedSize.y; y++)
            {
                for (int z = 0; z < paddedSize.z; z++)
                {
                    float3 p = new float3(
                        (position.x + x) * scale.x + offset.x,
                        (position.y + y) * scale.y + offset.y,
                        (position.z + z) * scale.z + offset.z
                    );
                    voxels[x + y * paddedSize.x + z * xy] = noise.cnoise(p);
                }
            }
        }
    }

    GenerateVoxels job;

    public VoxelGeneratorNoise(VoxelSettings settings) : base(settings)
    {
    }

    public override void Schedule()
    {
        NoiseSettings s = settings as NoiseSettings;

        job = new GenerateVoxels()
        {
            voxels = Volume.Voxels,
            paddedSize = Volume.PaddedSize,
            position = Volume.Bounds.min,
            scale = s.NoiseScale,
            offset = s.NoiseOffset
        };

        jobHandle = job.Schedule(Volume.PaddedSize.x, 32);
    }
}
