using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class ChunkManager : MonoBehaviour
{ public Transform player;
 
    public WorldChunk[] parcelPrefabs;
 
    public float chunkSize = 50f;
    public int renderDistance = 2;
    public int worldSeed = 12345;

    public int maxOpsPerFrame = 1;
    public float checkInterval = 0.25f;

    public int immediateLoadRadius = 1;


    bool _navMeshDirty;
float _lastBakeTime;
[SerializeField] NavMeshSurface navMeshSurface;

    public static ChunkManager Instance { get; private set; }
    // Ilk bake (asenkron) bitene kadar true. EnemySpawner gibi sistemler
    // sabit bir sure beklemek yerine bunu bekleyip gercekten navmesh hazir
    // olunca spawn'a baslamali.
    public bool FirstBakeDone { get; private set; }

    void Awake() => Instance = this;
 
    // K, D, G, B (yukarıdaki pathEdges sırasıyla aynı)
    static readonly Vector2Int[] Dirs = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };
    static readonly int[] OppositeEdge = { 2, 3, 0, 1 }; 
 
    readonly Dictionary<Vector2Int, int> committedParcel = new();

    readonly Dictionary<Vector2Int, WorldChunk> activeChunks = new();
 
    Vector2Int currentPlayerChunk;
    float timer;
 
    readonly Queue<Vector2Int> toSpawn = new();
    readonly Queue<Vector2Int> toDespawn = new();
 
    void Start()
    {
         currentPlayerChunk = WorldToChunkCoord(player.position);

    for (int dx = -immediateLoadRadius; dx <= immediateLoadRadius; dx++)
        for (int dy = -immediateLoadRadius; dy <= immediateLoadRadius; dy++)
            SpawnChunk(currentPlayerChunk + new Vector2Int(dx, dy));

    RecalculateWantedChunks();

    if (navMeshSurface != null)
    {
        navMeshSurface.transform.position = player.position;
        StartCoroutine(BakeNavMeshAsync());
        _navMeshDirty = false;
        _lastBakeTime = Time.time;

        // ONEMLI: ilk bake, chunk'lar HENUZ tam yerlesmeden (ozellikle
        // restart'ta - PoolManager objeleri yeniden aktive ederken +
        // RecalculateWantedChunks kuyrugu maxOpsPerFrame ile yavas
        // dolarken) calisip EKSIK/PARCALI bir navmesh uretebiliyordu.
        // Sonuc: dusmanlar player'a bagli olmayan bir navmesh "ada"sinda
        // spawn olup yerinde donup kaliyordu ("toplu saldirmayan enemyler").
        // Ilk birkac saniye ekstra, zamanlanmis rebake'lerle navmesh'in
        // butun yuklenen chunk'lari kapsamasini garantiliyoruz.
        StartCoroutine(EarlyRebakes());
    }
    }

    IEnumerator EarlyRebakes()
    {
        float[] delays = { 1f, 2.5f, 4.5f };
        foreach (float d in delays)
        {
            yield return new WaitForSeconds(d);
            if (navMeshSurface != null)
            {
                navMeshSurface.transform.position = player.position;
                yield return StartCoroutine(BakeNavMeshAsync());
                _navMeshDirty = false;
                _lastBakeTime = Time.time;
            }
        }
    }

    // BuildNavMesh() senkron/bloklayan calisiyordu - chunkSize buyuyunce
    // taranacak alan da buyudugu icin tek frame'de saniyelerce surup donmaya
    // sebep oluyordu. UpdateNavMesh(), ayni bake islemini birkac frame'e
    // yayarak (AsyncOperation) yapar, ana thread'i bloklamaz.
    IEnumerator BakeNavMeshAsync()
    {
        if (navMeshSurface == null) yield break;
        // UpdateNavMesh, BuildNavMesh'in aksine sifirdan NavMeshData olusturmuyor,
        // var olan bir data bekliyor. Ilk bake'te bu henuz atanmamis (null) oluyordu.
        if (navMeshSurface.navMeshData == null)
            navMeshSurface.navMeshData = new NavMeshData();
        var op = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
        yield return op;

        // KRITIK: UpdateNavMesh datayi SADECE bake eder, runtime NavMesh
        // sistemine EKLEMEZ. NavMeshSurface.OnEnable icindeki AddData() ise
        // o an navMeshData null oldugu icin hicbir sey kaydetmemisti. Sonuc:
        // navmesh bake ediliyor ama HIC AKTIF OLMUYOR - SamplePosition/agent
        // hicbir sey bulamiyor (ozellikle restart'ta - "dusmanlar dibimde /
        // grupca donuyor" sorunlarinin KOKU buydu). RemoveData()+AddData() ile
        // taze bake edilmis datayi acikca kaydediyoruz.
        navMeshSurface.RemoveData();
        navMeshSurface.AddData();

        FirstBakeDone = true;
    }
 
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            Vector2Int newChunk = WorldToChunkCoord(player.position);
            if (newChunk != currentPlayerChunk)
            {
                currentPlayerChunk = newChunk;
                RecalculateWantedChunks();
            }
        }
 
        int ops = 0;
        while (ops < maxOpsPerFrame && toDespawn.Count > 0)
        {
            DespawnChunk(toDespawn.Dequeue());
            ops++;
        }
        while (ops < maxOpsPerFrame && toSpawn.Count > 0)
        {
            SpawnChunk(toSpawn.Dequeue());
            ops++;
        }
 
        if (_navMeshDirty && Time.time - _lastBakeTime > 2f)
        {
            _navMeshDirty = false;
            _lastBakeTime = Time.time;
            if (navMeshSurface != null)
            {
                navMeshSurface.transform.position = player.position;
                StartCoroutine(BakeNavMeshAsync());
            }
        }
    }
 
    void RecalculateWantedChunks()
    {
        var wanted = new HashSet<Vector2Int>();
        for (int dx = -renderDistance; dx <= renderDistance; dx++)
            for (int dy = -renderDistance; dy <= renderDistance; dy++)
                wanted.Add(currentPlayerChunk + new Vector2Int(dx, dy));
 
        foreach (var coord in activeChunks.Keys)
            if (!wanted.Contains(coord) && !toDespawn.Contains(coord))
                toDespawn.Enqueue(coord);
 
        foreach (var coord in wanted)
            if (!activeChunks.ContainsKey(coord) && !toSpawn.Contains(coord))
                toSpawn.Enqueue(coord);
    }
 
    void SpawnChunk(Vector2Int coord)
    {
        if (activeChunks.ContainsKey(coord)) return;
 
        int parcelIndex = ChooseParcelIndex(coord);
        committedParcel[coord] = parcelIndex;
 
        
        WorldChunk prefab = parcelPrefabs[parcelIndex];
        string key = prefab.name;
        Vector3 pos = new Vector3(coord.x * chunkSize + chunkSize / 2f, 0, coord.y * chunkSize + chunkSize / 2f);
 
        GameObject obj = PoolManager.Instance.Get(key, pos, Quaternion.identity);
        if (obj == null)
        {
            Debug.LogWarning($"[ChunkManager] '{key}' PoolManager'da kayıtlı değil! poolEntries listesine ekle.");
            return;
        }
 
        WorldChunk instance = obj.GetComponent<WorldChunk>();
        instance.transform.SetParent(transform);
        instance.Generate(coord, chunkSize, new System.Random(CoordSeed(coord)));
        activeChunks[coord] = instance;

        _navMeshDirty = true;
    }

    void DespawnChunk(Vector2Int coord)
    {
        if (!activeChunks.TryGetValue(coord, out var instance)) return;
 
          int parcelIndex = committedParcel[coord];
        string key = parcelPrefabs[parcelIndex].name;
 
        PoolManager.Instance.Return(key, instance.gameObject);
        activeChunks.Remove(coord);
        // committedParcel silinmiyor -> aynı koordinata dönünce aynı parsel üretilsin
        _navMeshDirty = true;
    }

    int ChooseParcelIndex(Vector2Int coord)
    {
        if (committedParcel.TryGetValue(coord, out var existing))
            return existing;
 
        var rng = new System.Random(CoordSeed(coord));
 
        bool[] required = new bool[4];
        bool[] constrained = new bool[4];
 
        for (int edge = 0; edge < 4; edge++)
        {
            Vector2Int neighborCoord = coord + Dirs[edge];
            if (committedParcel.TryGetValue(neighborCoord, out var neighborParcelIdx))
            {
                bool neighborHasPathTowardsUs = parcelPrefabs[neighborParcelIdx].pathEdges[OppositeEdge[edge]];
                required[edge] = neighborHasPathTowardsUs;
                constrained[edge] = true;
            }
        }
 
        var candidates = new List<int>();
        for (int i = 0; i < parcelPrefabs.Length; i++)
        {
            bool ok = true;
            for (int edge = 0; edge < 4 && ok; edge++)
                if (constrained[edge] && parcelPrefabs[i].pathEdges[edge] != required[edge])
                    ok = false;
 
            if (ok) candidates.Add(i);
        }
 
        // Hiçbir prefab tüm şartlara uymuyorsa (nadiren olur), kısıtı gevşetip
        // en azından rastgele bir tane seç -> oyunun tamamen tıkanmasını engeller
        if (candidates.Count == 0)
            for (int i = 0; i < parcelPrefabs.Length; i++) candidates.Add(i);
 
        return candidates[rng.Next(candidates.Count)];
    }
 
    int CoordSeed(Vector2Int coord) => coord.x * 92821 + coord.y * 68917 + worldSeed;
 
    Vector2Int WorldToChunkCoord(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / chunkSize);
        int y = Mathf.FloorToInt(worldPos.z / chunkSize);
        return new Vector2Int(x, y);
    }
 
     void OnDrawGizmosSelected()
    {
        if (activeChunks == null) return;
        Gizmos.color = Color.yellow;
        foreach (var coord in activeChunks.Keys)
        {
            Vector3 center = new Vector3(coord.x * chunkSize + chunkSize / 2f, 0, coord.y * chunkSize + chunkSize / 2f);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, 1, chunkSize));
        }
    }
    
}

 