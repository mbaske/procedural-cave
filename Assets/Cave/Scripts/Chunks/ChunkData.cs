using UnityEngine;
using System.Collections.Generic;

public class ChunkData : System.IDisposable
{
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    // Multiple cave systems: ChunkData can be associated with more than one gameobject.
    public List<Chunk> AssociatedChunks { get; set; } = new List<Chunk>();
    public VoxelVolume Volume => voxelGen.Volume;
    public bool HasUpToDateMesh => state == State.Completed;
    public Mesh Mesh => meshGen.Mesh;

    private enum State
    {
        Idle = 0,
        VoxelsScheduled = 1,
        MeshScheduled = 2,
        Completed = 3
    }
    private State state;
    private MeshGenerator meshGen;
    private VoxelGenerator voxelGen;

    public ChunkData(VoxelSettings voxelSettings, MeshSettings meshSettings)
    {
        voxelGen = VoxelGeneratorFactory.CreateInstance(voxelSettings);
        meshGen = MeshGeneratorFactory.CreateInstance(meshSettings);
        meshGen.VoxelGenerator = voxelGen;
    }

    public void SetPosition(Vector3Int pos)
    {
        voxelGen.Volume.SetPosition(pos);
        state = State.Idle;
    }

    public bool IsCompleted(ref int availableWorkers)
    {
        switch (state)
        {
            case State.Idle:
                if (availableWorkers > 0)
                {
                    voxelGen.Schedule();
                    state = State.VoxelsScheduled;
                    availableWorkers--;
                }
                break;

            case State.VoxelsScheduled:
                if (voxelGen.IsCompleted)
                {
                    // TODO Schedule with dependency?
                    voxelGen.Complete();
                    meshGen.Schedule();
                    state = State.MeshScheduled;
                }
                break;

            case State.MeshScheduled:
                if (meshGen.IsCompleted)
                {
                    meshGen.Complete();
                    state = State.Completed;
                    availableWorkers++;

                    foreach (Chunk chunk in AssociatedChunks)
                    {
                        chunk.SetActive(true, meshGen.Mesh);
                    }
                }
                break;
        }

        return state == State.Completed;
    }

    public void CancelJobs(ref int availableWorkers)
    {
        switch (state)
        {
            case State.VoxelsScheduled:
                voxelGen.Complete();
                availableWorkers++;
                break;

            case State.MeshScheduled:
                meshGen.Complete();
                availableWorkers++;
                break;
        }
    }

    public void Dispose()
    {
        voxelGen.Dispose();
        meshGen.Dispose();
    }
}
