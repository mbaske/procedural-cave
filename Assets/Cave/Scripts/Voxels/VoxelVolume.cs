using UnityEngine;
using Unity.Collections;

public class VoxelVolume : System.IDisposable
{
    public Bounds Bounds = new Bounds();
    public Vector3Int Position { get; private set; }
    public Vector3Int Size { get; private set; }
    public Vector3Int PaddedSize { get; private set; }
    public int VoxelCount { get; private set; }
    public NativeArray<float> Voxels { get; private set; }

    public VoxelVolume(VoxelSettings settings)
    {
        Size = settings.VoxelsPerChunk;
        Bounds.size = Size;
        PaddedSize = Size + Vector3Int.one;
        VoxelCount = PaddedSize.x * PaddedSize.y * PaddedSize.z;
        Voxels = new NativeArray<float>(VoxelCount, Allocator.Persistent);
    }

    public void SetPosition(Vector3Int pos)
    {
        Bounds.center = pos;
        Position = pos;
    }

    public void Dispose()
    {
        Voxels.Dispose();
    }
}
