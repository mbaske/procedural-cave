using System.Collections.Generic;
using UnityEngine;

public class ChunkPool : MonoBehaviour, System.IDisposable
{
    // Global frame counter for staggered chunk updates.
    public static int GetBusyCount()
    {
        return busyCount++;
    }
    private static int busyCount;
    private static void OnFrameUpdate()
    {
        busyCount = Mathf.Max(0, --busyCount);
    }

    // Scriptable object instance.
    [SerializeField]
    private VoxelSettings voxelSettings;
    // Scriptable object instance.
    [SerializeField]
    private MeshSettings meshSettings;
    [SerializeField]
    private Chunk chunkPrefab;
    // Number of jobs that can be scheduled simultaneously.
    [SerializeField]
    private int numWorkers = 32;

    private Dictionary<Vector3Int, ChunkData> dataByPos;
    private List<ChunkData> bufferedData;
    private Queue<ChunkData> reusableData;
    private List<ChunkData> scheduledData;

    private Dictionary<string, Chunk> chunksByName;
    private Queue<Chunk> reusableChunks;

    private void Awake()
    {
        dataByPos = new Dictionary<Vector3Int, ChunkData>();
        bufferedData = new List<ChunkData>();
        reusableData = new Queue<ChunkData>();
        scheduledData = new List<ChunkData>();

        chunksByName = new Dictionary<string, Chunk>();
        reusableChunks = new Queue<Chunk>();

        InvokeRepeating("FlushBuffered", 1, 1);
    }

    public Chunk Activate(Vector3Int pos, Transform parent, int priority)
    {
        string name = Chunk.GetName(parent, pos);

        if (!dataByPos.TryGetValue(pos, out ChunkData data))
        {
            data = reusableData.Count > 0
                ? reusableData.Dequeue()
                : new ChunkData(voxelSettings, meshSettings);
            data.CancelJobs(ref numWorkers);
            data.SetPosition(pos);
            data.Priority = priority;
            dataByPos.Add(pos, data);
            scheduledData.Add(data);
        }

        if (!chunksByName.TryGetValue(name, out Chunk chunk))
        {
            chunk = reusableChunks.Count > 0
                ? reusableChunks.Dequeue()
                : Instantiate(chunkPrefab);
            chunk.Place(name, parent, data.Volume.Bounds.min);
            chunksByName.Add(name, chunk);
        }

        data.IsActive = true;
        data.AssociatedChunks.Add(chunk);
        chunk.SetActive(data.HasUpToDateMesh, data.Mesh);

        return chunk;
    }

    public void SortByPriority()
    {
        // Priority value is the negative distance^2 between agent and chunk pos.
        // Smallest distance (0) -> highest priority data is placed last in the list.
        // The Update loop checks list items in descending order.
        // We're not using a queue, because jobs are not guaranteed to finish in 
        // the same order they were scheduled.
        scheduledData.Sort((x, y) => x.Priority.CompareTo(y.Priority));
    }

    public Chunk Deactivate(Vector3Int pos, Transform parent)
    {
        string name = Chunk.GetName(parent, pos);
        Chunk chunk = chunksByName[name];
        chunk.SetActive(false);
        chunksByName.Remove(name);
        reusableChunks.Enqueue(chunk);

        ChunkData data = dataByPos[pos];
        data.AssociatedChunks.Remove(chunk);
        if (data.AssociatedChunks.Count == 0)
        {
            data.IsActive = false;
            if (!bufferedData.Contains(data))
            {
                bufferedData.Add(data);
            }
        }

        return chunk;
    }

    private void FlushBuffered()
    {
        foreach (ChunkData data in bufferedData)
        {
            if (!data.IsActive)
            {
                dataByPos.Remove(data.Volume.Position);
                reusableData.Enqueue(data);
            }
        }
        bufferedData.Clear();
    }

    public void Dispose()
    {
        foreach (ChunkData data in dataByPos.Values)
        {
            data.CancelJobs(ref numWorkers);
            data.Dispose();
        }
        foreach (ChunkData data in reusableData)
        {
            data.CancelJobs(ref numWorkers);
            data.Dispose();
        }

        MeshLookup.GetInstance().Dispose();
    }

    private void Update()
    {
        OnFrameUpdate();

        for (int i = scheduledData.Count - 1; i >= 0; i--)
        {
            if (scheduledData[i].IsCompleted(ref numWorkers))
            {
                scheduledData.RemoveAt(i);
            }
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
