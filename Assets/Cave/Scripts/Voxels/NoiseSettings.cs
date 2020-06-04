using UnityEngine;

[CreateAssetMenu(fileName = "NoiseSettings", menuName = "ScriptableObjects/NoiseSettings", order = 1)]
public class NoiseSettings : VoxelSettings
{
    public Vector3 NoiseScale = Vector3.one * 0.1f;
    public Vector3 NoiseOffset = Vector3.zero;
}
