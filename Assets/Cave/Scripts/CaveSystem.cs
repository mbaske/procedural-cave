using System.Collections.Generic;
using UnityEngine;

public class CaveSystem : MonoBehaviour
{
    // Scriptable object instance.
    [SerializeField]
    private VoxelSettings voxelSettings;
    [SerializeField, Tooltip("Number of chunks extending from center chunk")]
    private Vector3Int extents;

    [SerializeField]
    private float updateDistance = 5;
    private float updateSqrDistance;
    private Vector3 prevAgentPos;
    // Update on rotation when using cam frustum.
    [SerializeField]
    private float updateAngle = 5; 
    private Vector3 prevAgentFwd;

    private Camera cam;
    private Plane[] planes;
    private bool useCamFrustum;

    private Transform agent;
    private ChunkPool chunkPool;
    private BoundsCollection bounds;
    private Vector3Int chunkSize;
    private Vector3 invChunkSize;
    private HashSet<Vector3Int> coords;
    private HashSet<Chunk> chunks;
    private bool allChunksActive;
    private bool isInitialized;

    public void Initialize()
    {
        chunkPool = FindObjectOfType<ChunkPool>();
        chunks = new HashSet<Chunk>();
        coords = new HashSet<Vector3Int>();
        
        chunkSize = voxelSettings.VoxelsPerChunk;
        invChunkSize = new Vector3(
            1f / (float)chunkSize.x,
            1f / (float)chunkSize.y,
            1f / (float)chunkSize.z);
        bounds = new BoundsCollection(chunkSize, extents);
        updateSqrDistance = updateDistance * updateDistance;
        isInitialized = true;
    }

    public void Initialize(Transform agent)
    {
        this.agent = agent;
        Initialize();
    }

    public void Initialize(Transform agent, Camera camera)
    {
        planes = new Plane[6];
        useCamFrustum = true;
        cam = camera;
        Initialize(agent);
    }

    public void OnReset()
    {
        foreach (Vector3Int coord in coords)
        {
            chunks.Remove(chunkPool.Deactivate(coord, transform));
        }
        coords.Clear();
        allChunksActive = false;
        prevAgentPos = Vector3.one * 999;
        prevAgentFwd = agent.forward;
    }

    public bool AllChunksActive()
    {
        if (allChunksActive)
        {
            return true;
        }
        else if (chunks.Count == 0)
        {
            return false;
        }

        allChunksActive = true;
        foreach (Chunk chunk in chunks)
        {
            allChunksActive = allChunksActive && chunk.IsActive;
        }
        return allChunksActive;
    }

    private void Update()
    {
        if (isInitialized && MustUpdate())
        {
            // Round agent pos to valid chunk pos.
            Vector3Int roundedPos = Vector3Int.Scale(Vector3Int.RoundToInt(
                Vector3.Scale(agent.localPosition, invChunkSize)), chunkSize);
            HashSet<Vector3Int> tmp = new HashSet<Vector3Int>(coords);
            coords.Clear();

            foreach (Bounds b in bounds.GetBounds(roundedPos))
            {
                Vector3Int v = Vector3Int.RoundToInt(b.center); 
                if (!useCamFrustum || GeometryUtility.TestPlanesAABB(planes, b))
                {
                    coords.Add(v);
                    if (!tmp.Contains(v))
                    {
                        int priority = -(v - roundedPos).sqrMagnitude;
                        chunks.Add(chunkPool.Activate(v, transform, priority));
                        allChunksActive = false;
                    }
                }
            }

            tmp.ExceptWith(coords);
            foreach (Vector3Int v in tmp)
            {
                chunks.Remove(chunkPool.Deactivate(v, transform));
            }
        }
    }

    private bool MustUpdate()
    {
        if ((agent.position - prevAgentPos).sqrMagnitude > updateSqrDistance || 
            (useCamFrustum && Vector3.Angle(agent.forward, prevAgentFwd) > updateAngle))
        {
            prevAgentPos = agent.position;

            if (useCamFrustum)
            {
                prevAgentFwd = agent.forward;
                GeometryUtility.CalculateFrustumPlanes(cam, planes);
            }
            return true;
        }
        return false;
    }
}