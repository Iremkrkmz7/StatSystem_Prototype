using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChunk : MonoBehaviour , IPoolable
{
    [SerializeField] GameObject[] propVariants;
    [Tooltip("Bu spawn point'leri hangi chunk boyutunda yerlestirdin (Scene view'da surukleyip biraktigin an). Chunk boyutu buyuyup kuculdukce noktalar bu referansa gore orantili olceklenir - tasarladigin yerlesim korunur, bosluk olusmaz.")]
    [SerializeField] float referenceChunkSize = 50f;
    [SerializeField] Transform[]  spawnPoints;

    [Tooltip("spawnPoints bossa yedek olarak kullanilir: birim alan basina ortalama prop sayisi.")]
    [SerializeField] float propDensity = 0.0016f;
    [Tooltip("Chunk ne kadar buyurse buyusun, tek chunk'ta spawn edilecek maksimum prop sayisi (donmayi onlemek icin guvenlik siniri, sadece yedek rastgele modda gecerli).")]
    [SerializeField] int maxPropsPerChunk = 60;
    [SerializeField] float propEdgeMargin = 4f;

    [SerializeField] GameObject[] groundDecorations;
    [SerializeField] int minDecorations = 0;
    [SerializeField] int maxDecorations = 2;

    [SerializeField] float decorationEdgeMargin = 6f;

    public bool[] pathEdges = new bool[4];

    
    public Vector2Int ChunkCoord { get; private set; }

    readonly List<(string key, GameObject instance)> active = new();

     public void Generate(Vector2Int coord, float chunkSize, System.Random rng)
    {
        ChunkCoord = coord;
 
        if (propVariants.Length > 0 && spawnPoints.Length > 0)
        {
           
            float scale = chunkSize / referenceChunkSize;
            foreach (Transform point in spawnPoints)
            {
                if (point == null) continue;
                Vector3 pos = transform.position + point.localPosition * scale;
                var rot = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);

                var prefab = propVariants[rng.Next(propVariants.Length)];
                SpawnFromPool(prefab, pos, rot);
            }
        }
        else if (propVariants.Length > 0)
        {
            
            int propCount = Mathf.Min(Mathf.RoundToInt(chunkSize * chunkSize * propDensity), maxPropsPerChunk);
            float half = chunkSize / 2f - propEdgeMargin;

            for (int i = 0; i < propCount; i++)
            {
                float offsetX = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float offsetZ = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                Vector3 pos = transform.position + new Vector3(offsetX, 0f, offsetZ);
                var rot = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);

                var prefab = propVariants[rng.Next(propVariants.Length)];
                SpawnFromPool(prefab, pos, rot);
            }
        }
 
        if (groundDecorations.Length > 0)
        {
            int decorCount = rng.Next(minDecorations, maxDecorations + 1);
            float half = chunkSize / 2f - decorationEdgeMargin;
 
            for (int i = 0; i < decorCount; i++)
            {
                float offsetX = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float offsetZ = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                Vector3 pos = transform.position + new Vector3(offsetX, 0f, offsetZ);
                var rot = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
 
                var prefab = groundDecorations[rng.Next(groundDecorations.Length)];
                SpawnFromPool(prefab, pos, rot);
            }
        }
    }
 
   
    GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        string key = prefab.name;
        GameObject obj = PoolManager.Instance.Get(key, pos, rot);
        if (obj == null)
        {
            Debug.LogWarning($"[WorldChunk] '{key}' PoolManager'da kayıtlı değil! poolEntries listesine ekle.");
            return null;
        }
        obj.transform.SetParent(transform);
        active.Add((key, obj));
        return obj;
    }
 
    public void OnSpawnFromPool() { }
 
    public void OnReturnToPool()
    {
        foreach (var (key, instance) in active)
        {
            if (instance == null) continue;
            PoolManager.Instance.Return(key, instance);
        }
        active.Clear();
    }
}