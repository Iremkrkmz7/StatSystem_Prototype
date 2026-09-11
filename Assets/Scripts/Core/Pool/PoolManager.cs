using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
     void OnSpawnFromPool();
     void OnReturnToPool();
}
public class PoolManager : MonoBehaviour
{
   public static PoolManager Instance{get; private set; }
    [System.Serializable]
    public class PoolEntry
    {
        public string Key;
        public GameObject Prefab;
        public int InitialSize = 10;
    }
    [SerializeField] List<PoolEntry> poolEntries = new();

    readonly Dictionary<string, Queue<GameObject>> _pools = new();
    readonly Dictionary<string, GameObject> _prefabs = new();

    void Awake()
    {
        if(Instance != null) {Destroy(gameObject); return;}
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePools();
    }
      void InitializePools()
    {
        foreach(var entry in poolEntries)
        {
            _prefabs[entry.Key] = entry.Prefab;
            var queue = new Queue<GameObject>();
            _pools[entry.Key] = queue;

            for(int i = 0; i< entry.InitialSize; i++)
            {
                var obj = Instantiate(entry.Prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
        }
    }
      public GameObject Get(string key, Vector3 pos, Quaternion rot)
    {
        if(!_pools.TryGetValue(key, out var queue))
        {
           Debug.LogWarning($"[PoolManager] '{key}' bulunamadı!");
            return null;
        }
        // Guvenlik payi: queue'daki referans her nasilsa BOZUK/SILINMIS
        // (Unity'nin "fake null" Destroy edilmis objesi) cikarsa onu atlayip
        // devam ediyoruz - tek bir kokten referans yuzunden tum cagiran
        // dongunun (orn. ChunkManager'in chunk spawn loop'u) YARIDA kesilip
        // haritada delik birakmasini onler.
        GameObject obj = null;
        while (queue.Count > 0)
        {
            var candidate = queue.Dequeue();
            if (candidate != null) { obj = candidate; break; }
        }
        if (obj == null) obj = Instantiate(_prefabs[key], transform);

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawnFromPool();
        return obj;
    }
    public void Return(string key, GameObject obj)
    {
        if(obj == null) return;
        obj.GetComponent<IPoolable>()?.OnReturnToPool();
        obj.SetActive(false);

        if(_pools.TryGetValue(key, out var queue))
        {
            // ONEMLI: obje "checkout" edilirken (Get() sonrasi) genelde baska
            // bir sahne objesine (orn. ChunkManager) reparent ediliyordu -
            // burada PoolManager'in KENDI transformuna (DontDestroyOnLoad)
            // GERI reparent ETMEDEN sadece deactivate+enqueue edip birakiyorduk.
            // Sonuc: obje hala eski (DontDestroyOnLoad OLMAYAN) parent'in
            // altinda kaliyordu - sahne yeniden yuklendiginde (restart) o eski
            // parent (ve onunla birlikte, queue'daki "pooled" obje de) YOK
            // OLUYORDU, ama PoolManager'in queue'su hala o artik SILINMIS
            // objeye referans tutuyordu. Restart sonrasi Get() bu "olu"
            // referansi cikarinca MissingReferenceException firliyor, bu da
            // ChunkManager.Start()'daki chunk spawn dongusunu YARIDA KESIP
            // haritada bosluklar/delikler birakiyordu. Simdi PoolManager'in
            // KENDI (kalici) transformuna geri alip bu sorunu kokten cozuyoruz.
            obj.transform.SetParent(transform);
            queue.Enqueue(obj);
        }
        else
        Destroy(obj);
    }
}
     

