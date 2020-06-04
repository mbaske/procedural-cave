using UnityEngine;
using System.Collections.Generic;

public class BoundsCollection
{
    private Vector3Int size;
    private Vector3Int scaledExtents;
    private Dictionary<Vector3Int, Bounds> buffer;

    public BoundsCollection(Vector3Int voxelsPerChunk, Vector3Int extents)
    {
        size = voxelsPerChunk;
        scaledExtents = Vector3Int.Scale(size, extents);
        buffer = new Dictionary<Vector3Int, Bounds>();
    }

    public IEnumerable<Bounds> GetBounds(Vector3Int center)
    {
        Vector3Int min = center - scaledExtents;
        Vector3Int max = center + scaledExtents;

        for (int x = min.x; x <= max.x; x += size.x)
        {
            for (int y = min.y; y <= max.y; y += size.y)
            {
                for (int z = min.z; z <= max.z; z += size.z)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!buffer.TryGetValue(pos, out Bounds bounds))
                    {
                        bounds = new Bounds(pos, size);
                        buffer.Add(pos, bounds);
                    }
                    yield return bounds;
                }
            }
        }
    }
}
