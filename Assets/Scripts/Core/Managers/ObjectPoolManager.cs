using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PrisonLife.Core
{
    /// <summary>
    /// Centralized object-pool manager for the Arcade Idle game.
    /// Provides spawn/despawn API for pooled prefabs such as flying-rock visuals,
    /// flying-handcuff visuals, criminal NPCs, and VFX.
    /// Backed by <see cref="UnityEngine.Pool.ObjectPool{T}"/> (Unity 2021+).
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Singleton

        public static ObjectPoolManager Instance { get; private set; }

        #endregion

        #region Nested Types

        /// <summary>
        /// Inspector-friendly definition for a single pool.
        /// Configure tag, prefab, initial capacity, and whether the pool
        /// is allowed to grow at runtime when exhausted.
        /// </summary>
        [System.Serializable]
        public class PoolDefinition
        {
            [Tooltip("Unique identifier used to request objects from this pool.")]
            public string tag;

            [Tooltip("Prefab that will be instantiated into this pool.")]
            public GameObject prefab;

            [Tooltip("Number of instances to pre-instantiate on Awake.")]
            public int initialSize = 10;

            [Tooltip("Maximum number of inactive objects kept in the pool. -1 = unlimited.")]
            public int maxSize = 100;

            [Tooltip("If true, the pool will instantiate new objects when exhausted instead of returning null.")]
            public bool expandable = true;
        }

        /// <summary>
        /// Compile-time constant pool tags.
        /// Use these instead of magic strings to avoid typos and enable refactor support.
        /// </summary>
        public static class PoolTags
        {
            public const string FlyingRock     = "FlyingRock";
            public const string FlyingHandcuff = "FlyingHandcuff";
            public const string Criminal       = "Criminal";
            public const string VFX_MinePoof   = "VFX_MinePoof";
        }

        /// <summary>Internal wrapper that pairs a Unity ObjectPool with its metadata.</summary>
        private class PoolEntry
        {
            public PoolDefinition Definition;
            public ObjectPool<GameObject> Pool;
            public Transform Parent;
        }

        #endregion

        #region Serialized Fields

        [Header("Pool Definitions")]
        [Tooltip("Add one entry per prefab type you want pooled. Configure in the Inspector.")]
        [SerializeField] private List<PoolDefinition> _poolDefinitions = new List<PoolDefinition>();

        #endregion

        #region Private Fields

        /// <summary>Maps tag → PoolEntry (Unity ObjectPool + metadata).</summary>
        private Dictionary<string, PoolEntry> _entries;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // --- Singleton enforcement ---
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializePools();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Builds one <see cref="ObjectPool{T}"/> per <see cref="PoolDefinition"/> and
        /// pre-warms each pool to <see cref="PoolDefinition.initialSize"/>.
        /// </summary>
        private void InitializePools()
        {
            _entries = new Dictionary<string, PoolEntry>();

            foreach (PoolDefinition def in _poolDefinitions)
            {
                if (string.IsNullOrEmpty(def.tag) || def.prefab == null)
                {
                    Debug.LogWarning($"[ObjectPoolManager] Skipping invalid pool definition (tag='{def.tag}').");
                    continue;
                }

                // Hierarchy container.
                GameObject parentGO = new GameObject($"Pool_{def.tag}");
                parentGO.transform.SetParent(transform);
                Transform parent = parentGO.transform;

                // Capture locals for the lambdas below.
                PoolDefinition capturedDef = def;
                Transform      capturedParent = parent;

                int maxSize = def.maxSize > 0 ? def.maxSize : int.MaxValue;

                ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                    createFunc:      () =>
                    {
                        GameObject go = Instantiate(capturedDef.prefab, capturedParent);
                        go.SetActive(false);
                        return go;
                    },
                    actionOnGet:     go =>
                    {
                        go.SetActive(true);
                    },
                    actionOnRelease: go =>
                    {
                        go.SetActive(false);
                        go.transform.SetParent(capturedParent);
                    },
                    actionOnDestroy: go => Destroy(go),
                    collectionCheck: false,
                    defaultCapacity: def.initialSize,
                    maxSize:         maxSize
                );

                _entries[def.tag] = new PoolEntry
                {
                    Definition = def,
                    Pool       = pool,
                    Parent     = parent,
                };

                // Pre-warm: get then release so objects are created and returned to the pool.
                var prewarm = new List<GameObject>(def.initialSize);
                for (int i = 0; i < def.initialSize; i++)
                    prewarm.Add(pool.Get());
                foreach (var go in prewarm)
                    pool.Release(go);
            }
        }

        #endregion

        #region Public API — Spawn

        /// <summary>
        /// Retrieves an inactive object from the pool identified by <paramref name="tag"/>,
        /// activates it, and places it at the given world position and rotation.
        /// If the pool is exhausted and marked <see cref="PoolDefinition.expandable"/>,
        /// a new instance is created on the fly.
        /// </summary>
        /// <param name="tag">Pool tag (use <see cref="PoolTags"/> constants).</param>
        /// <param name="position">World-space position to place the object.</param>
        /// <param name="rotation">World-space rotation to apply.</param>
        /// <returns>The activated <see cref="GameObject"/>, or <c>null</c> if the pool
        /// is exhausted and not expandable.</returns>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_entries.TryGetValue(tag, out PoolEntry entry))
            {
                Debug.LogError($"[ObjectPoolManager] Spawn: unknown pool tag '{tag}'.");
                return null;
            }

            // Unity's ObjectPool always creates a new object when exhausted (no hard cap
            // unless maxSize is set). We honour the expandable flag by checking count.
            if (!entry.Definition.expandable && entry.Pool.CountInactive == 0)
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' is exhausted and not expandable.");
                return null;
            }

            GameObject obj = entry.Pool.Get();
            obj.transform.SetParent(null);
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// Convenience overload — spawns the object at world origin with identity rotation.
        /// </summary>
        public GameObject Spawn(string tag)
        {
            return Spawn(tag, Vector3.zero, Quaternion.identity);
        }

        #endregion

        #region Public API — Despawn

        /// <summary>
        /// Deactivates <paramref name="obj"/> and returns it to the pool identified by
        /// <paramref name="tag"/>. Re-parents the object under the pool's parent transform.
        /// </summary>
        public void Despawn(string tag, GameObject obj)
        {
            if (!_entries.TryGetValue(tag, out PoolEntry entry))
            {
                Debug.LogError($"[ObjectPoolManager] Despawn: unknown pool tag '{tag}'.");
                return;
            }

            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPoolManager] Despawn: null object passed for pool '{tag}'.");
                return;
            }

            entry.Pool.Release(obj);
        }

        /// <summary>
        /// Deactivates and returns <paramref name="obj"/> to the pool after
        /// <paramref name="delay"/> seconds.
        /// </summary>
        public void Despawn(string tag, GameObject obj, float delay)
        {
            StartCoroutine(DespawnAfterDelay(tag, obj, delay));
        }

        /// <summary>
        /// Coroutine that waits <paramref name="delay"/> seconds then despawns.
        /// </summary>
        private IEnumerator DespawnAfterDelay(string tag, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn(tag, obj);
        }

        #endregion

        #region Public API — Pool Management

        /// <summary>
        /// Adds <paramref name="additionalCount"/> new inactive instances to the pool.
        /// </summary>
        public void ExpandPool(string tag, int additionalCount)
        {
            if (!_entries.TryGetValue(tag, out PoolEntry entry))
            {
                Debug.LogError($"[ObjectPoolManager] ExpandPool: unknown pool tag '{tag}'.");
                return;
            }

            var temp = new List<GameObject>(additionalCount);
            for (int i = 0; i < additionalCount; i++)
                temp.Add(entry.Pool.Get());
            foreach (var go in temp)
                entry.Pool.Release(go);
        }

        /// <summary>
        /// Returns <c>true</c> if the pool has at least one inactive object ready.
        /// </summary>
        public bool HasAvailable(string tag)
        {
            return _entries.TryGetValue(tag, out PoolEntry entry) && entry.Pool.CountInactive > 0;
        }

        /// <summary>
        /// Returns the total number of objects (active + inactive) created for this pool.
        /// </summary>
        public int GetPoolCount(string tag)
        {
            if (!_entries.TryGetValue(tag, out PoolEntry entry))
            {
                Debug.LogWarning($"[ObjectPoolManager] GetPoolCount: unknown pool tag '{tag}'.");
                return 0;
            }

            return entry.Pool.CountAll;
        }

        /// <summary>
        /// Pre-warms the pool with <paramref name="count"/> additional inactive instances.
        /// </summary>
        public void WarmUp(string tag, int count)
        {
            ExpandPool(tag, count);
        }

        #endregion
    }
}
