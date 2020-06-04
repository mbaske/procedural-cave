using UnityEngine;

public class Chunk : MonoBehaviour
{
    public static string GetName(Transform parent, Vector3Int pos)
    {
        return $"{parent.GetInstanceID()}_{pos.x}_{pos.y}_{pos.z}";
    }

    public bool IsActive { get; private set; }

    // Delayed option: avoid adding all mesh colliders at once.
    private enum AddColliderMode
    {
        Immediate = 0,
        Delayed = 1,
        DelayedOnReuse = 2,
        None = 3
    }

    [SerializeField]
    private AddColliderMode addCollider;
    [SerializeField]
    private float delay = 0.05f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.enabled = addCollider != AddColliderMode.None;
    }

    public void Place(string name, Transform parent, Vector3 localPos)
    {
        IsActive = false;
        transform.name = name;
        transform.parent = parent;
        transform.localPosition = localPos;
    }

    public void SetActive(bool active, Mesh mesh = null)
    {
        gameObject.SetActive(active);
        meshFilter.sharedMesh = mesh;

        if (active)
        {
            if (addCollider == AddColliderMode.Delayed)
            {
                Invoke("AddCollider", ChunkPool.GetBusyCount() * delay);
            }
            else if (addCollider == AddColliderMode.Immediate)
            {
                AddCollider();
            }
            else if (addCollider == AddColliderMode.DelayedOnReuse)
            {
                AddCollider();
                addCollider = AddColliderMode.Delayed;
            }
        }
        else
        {
            meshCollider.sharedMesh = null;
        }
    }

    private void AddCollider()
    {
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        IsActive = true;
    }
}
