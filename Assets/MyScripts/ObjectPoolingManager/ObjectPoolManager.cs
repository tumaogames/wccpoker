using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A centralized and extensible object pooling system.
/// Attach this to a persistent GameObject in the scene.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tagObj;
        public GameObject prefab;
        public int size = 10;
        public bool allowDynamicExpansion = false;
    }

    public static ObjectPoolManager Instance { get; private set; }

    [Header("Pool Definitions")]
    [Tooltip("Configure pools with unique tags and prefabs.")]
    public List<Pool> pools;

    private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, Pool> poolConfigs = new Dictionary<string, Pool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ObjectPoolManagers detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (Pool pool in pools)
        {
            if (string.IsNullOrWhiteSpace(pool.tagObj))
            {
                Debug.LogError("Pool tag is missing.");
                continue;
            }

            if (pool.prefab == null)
            {
                Debug.LogError($"Pool '{pool.tagObj}' has no prefab assigned.");
                continue;
            }

            if (poolDictionary.ContainsKey(pool.tagObj))
            {
                Debug.LogWarning($"Duplicate pool tag '{pool.tagObj}' found. Skipping.");
                continue;
            }

            var objectQueue = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.name = $"{pool.tagObj}_Pooled_{i}";
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                objectQueue.Enqueue(obj);
            }

            poolDictionary[pool.tagObj] = objectQueue;
            poolConfigs[pool.tagObj] = pool;
        }
    }

    /// <summary>
    /// Spawns a pooled object by tag.
    /// </summary>
    /// <param name="tag">Pool tag</param>
    /// <param name="position">Spawn position</param>
    /// <param name="rotation">Spawn rotation</param>
    /// <returns>Pooled GameObject or null if not found</returns>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.TryGetValue(tag, out var objectQueue))
        {
            Debug.LogError($"[ObjectPoolManager] No pool with tag '{tag}' found.");
            return null;
        }

        // Expand if pool is empty and allowed
        if (objectQueue.Count == 0)
        {
            if (poolConfigs.TryGetValue(tag, out var config) && config.allowDynamicExpansion)
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' exhausted. Expanding dynamically.");
                ExpandPool(tag, 1);
            }
            else
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{tag}' is empty and not allowed to expand.");
                return null;
            }
        }

        // Dequeue and validate the object
        GameObject objectToSpawn = objectQueue.Dequeue();

        // Replace nulls safely
        if (objectToSpawn == null)
        {
            Debug.LogWarning($"[ObjectPoolManager] Null object found in pool '{tag}'. Replacing...");
            if (!poolConfigs.TryGetValue(tag, out var config))
            {
                Debug.LogError($"[ObjectPoolManager] Missing config for tag '{tag}'.");
                return null;
            }

            objectToSpawn = Instantiate(config.prefab);
            objectToSpawn.name = $"{tag}_Pooled_Replaced";
        }

        objectToSpawn.transform.SetPositionAndRotation(position, rotation);
        objectToSpawn.SetActive(true);

        if (objectToSpawn.TryGetComponent<IPooledObject>(out var pooledObj))
        {
            pooledObj.OnObjectSpawn();
        }

        objectQueue.Enqueue(objectToSpawn);
        return objectToSpawn;
    }


    /// <summary>
    /// Dynamically adds more objects to a pool.
    /// </summary>
    public void ExpandPool(string tag, int count)
    {
        if (!poolConfigs.TryGetValue(tag, out var config))
        {
            Debug.LogError($"Cannot expand unknown pool: {tag}");
            return;
        }

        if (!poolDictionary.ContainsKey(tag))
            poolDictionary[tag] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(config.prefab);
            obj.name = $"{tag}_Expanded_{i}";
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[tag].Enqueue(obj);
        }

        Debug.Log($"[ObjectPoolManager] Expanded pool '{tag}' by {count} objects.");
    }

    /// <summary>
    /// Returns the number of objects currently in a pool.
    /// </summary>
    public int PoolSize(string tag)
    {
        return poolDictionary.TryGetValue(tag, out var q) ? q.Count : -1;
    }
}
