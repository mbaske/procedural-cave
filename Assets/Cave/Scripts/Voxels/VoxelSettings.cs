using UnityEngine;

public abstract class VoxelSettings : ScriptableObject
{
    public Vector3Int VoxelsPerChunk = Vector3Int.one * 10;
}