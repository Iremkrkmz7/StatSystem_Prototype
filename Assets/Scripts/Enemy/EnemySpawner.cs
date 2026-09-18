using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referans")]
  [SerializeField] Transform player;
  [SerializeField] EnemyDataSO[] enemyTypes;

  // Dalga ayarlari artik BalanceConfig'ten (GameBalance.xlsx -> CSV ->
  // Tools > Balance > Import). Sahnedeki eski SerializeField'lar kaldirildi.
  static BalanceConfig Cfg => BalanceConfig.Instance;
  int   baseEnemyCount    => Cfg.baseEnemyCount;
  int   countIncrement    => Cfg.countIncrement;
  int   maxWaveEnemyCount => Cfg.maxWaveEnemyCount;
  float spawnRadius       => Cfg.spawnRadius;
  float spawnInterval     => Cfg.spawnInterval;
  float waveCooldown      => Cfg.waveCooldown;
  float startupDelay      => Cfg.startupDelay;

  int _currentWave = 0;
  int _aliveEnemies = 0;

  // Tip basina kac tane canli oldugunu tutuyoruz - MaxAlive limiti ve
  // ileride tip bazli istatistik icin. SpawnOne() spawn basarili olunca
  // artirir, NotifyEnemyDied (EnemyAI olurken cagirir) azaltir.
  readonly Dictionary<EnemyDataSO, int> _aliveByType = new();

  public static EnemySpawner Instance { get; private set; }

  public int CurrentWave => _currentWave;
  public int AliveEnemies => _aliveEnemies;

  void Awake() => Instance = this;

  // EnemyAI, olurken (data referansiyla) bunu cagirir - GameEvents.OnEnemyKilled
  // parametresiz oldugu icin (HUDController/MultiKillUI de dinliyor, onu
  // bozmamak icin degistirmedik) tip bazli sayaci ayri, dogrudan bir
  // cagriyla azaltiyoruz.
  public void NotifyEnemyDied(EnemyDataSO data)
  {
      if (data == null) return;
      if (_aliveByType.TryGetValue(data, out int count))
          _aliveByType[data] = Mathf.Max(0, count - 1);
  }

  void Start()
  {
    if(player == null)
    player = GameObject.FindGameObjectWithTag("Player")?.transform;
    GameEvents.OnEnemyKilled.AddListener(OnEnemyKilled);

    // ONEMLI: PoolManager DontDestroyOnLoad - restart'ta (sahne reload)
    // O AN CANLI olan (havuza donmemis) eski dusmanlar reload'dan sag cikip
    // yeni oyunda player'in DIBINDE takili "zombi" olarak kaliyordu
    // ("bu seferde dibimdeler, daha bismillah baslamadan saldiriyorlar").
    // Yeni run baslarken hepsini sessizce havuza geri koyuyoruz.
    foreach (var leftover in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        leftover.ForceDespawn();

    // "Kaldigin yerden devam" - her yeni run (fresh Start VEYA Restart farketmez,
    // ikisinde de gecerli) en son ulasilan dalgadan (SaveSystem.BestWave)
    // devam eder, bastan (dalga 1) baslamaz. SpawnLoop() ilk cagrida
    // _currentWave++ yaptigi icin burada BestWave'i (bir sonraki, henuz
    // ulasilmamis dalgayi degil) atiyoruz - ilk spawn edilen dalga otomatik
    // BestWave+1 olur.
    _currentWave = SaveSystem.BestWave;

    // ONEMLI: Oyuncu seviyesi DALGA SAYISINDAN BAGIMSIZ, KENDI kayitli
    // rekorundan (SaveSystem.BestPlayerLevel) geliyor - ikisi FARKLI hizda
    // ilerliyor (biri dusman dalgasina, digeri XP'ye gore). Eskiden level'i
    // yanlislikla "dalga+1"den turetiyorduk - bu da "level 11'de oldum,
    // restart'ta 14'te basladi" gibi TUTARSIZ sonuclara sebep oluyordu
    // (dalga sayisi XP'den daha ileri gitmisse level de yanlislikla ona
    // gore sisiyordu). Artik GERCEK kayitli seviye kullaniliyor.
    int startLevel = SaveSystem.BestPlayerLevel;
    if (startLevel > 1)
    {
        XPSystem.Instance?.SetLevelSilently(startLevel);
        player?.GetComponent<SCharacterStats>()?.ApplyCatchUpLevels(startLevel);
    }

    StartCoroutine(DelayedStart());
  }

  IEnumerator DelayedStart()
  {
    // ONEMLI - BASITLESTIRILDI: eskiden burada ChunkManager'in NavMesh bake'ini
    // VE PlayerIntroDrop'un "havada mi" bayragini kontrol eden KOSULLU bekleme
    // vardi. Bu, cesitli durumlarda (ozellikle restart) beklenmedik sekilde HIC
    // cozulmeyip dusmanlarin bir turlu spawn olmamasina sebep oluyordu -
    // oyunda kabul edilemez bir sey. Simdi SABIT/BASIT bir bekleme kullaniyoruz:
    // once gercek oyun baslasin (Time.timeScale>0) diye bekliyoruz, sonra
    // startupDelay kadar (unscaled - Loading/Start Menu'de deltaTime ilerlemez)
    // sabit bir sure bekleyip HER ZAMAN, ongorulebilir sekilde spawn'a basliyoruz.
    // NavMesh henuz tam hazir olmasa bile SpawnOne() artik genis arama
    // yaricapi + retry + fallback ile bunu tolere ediyor.
    yield return new WaitUntil(() => Time.timeScale > 0f);

    // Sinematik giris + (ilk Start'ta) How To Play paneli TAMAMEN bitene kadar
    // bekle - yoksa bunlarin toplam suresi startupDelay'den uzun surdugunde
    // dusmanlar panel/sinematik arkasinda spawn olup oyuncuya ulasiyor, panel
    // kapanir kapanmaz aniden hasar/olum oluyordu. Guvenlik icin max 20sn -
    // ControlReturned herhangi bir sebeple hic true olmazsa bile dusmanlar
    // SONSUZA kadar spawn olmadan kalmasin.
    float controlWait = 0f;
    const float maxControlWait = 20f;
    while (!PlayerIntroDrop.ControlReturned && controlWait < maxControlWait)
    {
        yield return null;
        controlWait += Time.unscaledDeltaTime;
    }

    float waited = 0f;
    while (waited < startupDelay)
    {
        yield return null;
        waited += Time.unscaledDeltaTime;
    }

    StartCoroutine(SpawnLoop());
  }

  IEnumerator SpawnLoop()
  {
    while(true)
    {
        _currentWave++;
        int count = Mathf.Min(baseEnemyCount + (_currentWave - 1) * countIncrement, maxWaveEnemyCount);
        yield return StartCoroutine(SpawnWave(count));
        yield return new WaitUntil(() => _aliveEnemies <= 0);
        yield return new WaitForSeconds(waveCooldown);
    }
  }
  IEnumerator SpawnWave(int count)
  {
    for(int i = 0; i< count; i++)
    {
        SpawnOne();
        yield return new WaitForSeconds(spawnInterval);
    }

  }
  // Her cagrida yeniden hesaplaniyor (kucuk bir liste, performans onemli
  // degil) - MinWave henuz gelmemis VEYA MaxAlive limitine ulasmis tipleri
  // disarida birakir. Ornegin Bomber MinWave=4 ise ilk 3 dalgada hic cikmaz,
  // 4. dalgadan itibaren (MaxAlive'a kadar, SpawnWeight'e gore orantili
  // sansla) havuza girer - "pat diye bir suru" gelmesin diye.
  List<EnemyDataSO> _eligibleBuffer = new List<EnemyDataSO>();

  // Son spawn edilen birkac noktayi hatirliyoruz - NavMesh.SamplePosition
  // FARKLI aci/rastgele noktalari bile (navmesh dar/parcali bir alansa) hep
  // AYNI "en yakin gecerli nokta"ya sabitleyebiliyordu, bu da butun dalganin
  // tek bir yigin halinde ust uste spawn olmasina sebep oluyordu ("suru
  // halinde tek noktada" sikayeti buradan). Yeni bir nokta, bu listedeki
  // herhangi birine COK yakinsa reddedip baska bir aci deniyoruz.
  readonly List<Vector3> _recentSpawnPositions = new();
  const float minSpawnSeparation = 3f;
  const int maxRecentSpawnPositions = 20;
  float _spawnAngleCursor;  // altin aci ile donerek adaylari player etrafina dagitir

  void SpawnOne()
  {
    if(enemyTypes.Length == 0) return;

    int playerLevel = XPSystem.Instance != null ? XPSystem.Instance.CurrentLevel : 1;

    _eligibleBuffer.Clear();
    foreach (var t in enemyTypes)
    {
        if (t == null || t.MinWave > _currentWave) continue;
        if (t.MinPlayerLevel > playerLevel) continue;
        if (t.MaxAlive > 0 && _aliveByType.TryGetValue(t, out int aliveCount) && aliveCount >= t.MaxAlive) continue;
        _eligibleBuffer.Add(t);
    }
    if (_eligibleBuffer.Count == 0) return;

    var data = PickWeighted(_eligibleBuffer);

    // Adaylari player'in ETRAFINA ACISAL olarak dagitiyoruz (altin aci ~137.5
    // ile donuyor) - saf rastgele yerine bu, ust uste yigilmayi kokten azaltir.
    const int maxAttempts = 10;
    Vector3 pos = Vector3.zero;
    bool found = false;
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        _spawnAngleCursor = (_spawnAngleCursor + 137.508f) % 360f;
        float ang = (_spawnAngleCursor + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
        float r = spawnRadius * Random.Range(0.85f, 1.15f);
        Vector3 candidate = player.position + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);

        // Arama yaricapini KUCUK tutuyoruz (5) - boylece aday SADECE gercekten
        // o yonde navmesh varsa "tutuyor". Genis yaricap (eski 30), farkli
        // acilardaki adaylarin HEPSINI ayni yakindaki tek navmesh parcasina
        // "cekip" yigin olusturuyordu. Tutmazsa baska aci deneriz.
        if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            bool tooClose = false;
            foreach (var recent in _recentSpawnPositions)
                if (Vector3.Distance(hit.position, recent) < minSpawnSeparation) { tooClose = true; break; }
            if (tooClose) continue;

            pos = hit.position;
            found = true;
            break;
        }
    }

    if (!found)
    {
        // Hicbir acida yakinda navmesh bulunamadi (restart'ta parcali/eksik
        // bake) - RAW bir noktaya (navmesh'siz) spawn ediyoruz, yine ACISAL
        // dagitarak. Enemy'nin _directMove fallback'i (EnemyAI) navmesh
        // olmasa bile onu player'a dogru yurutup saldirtir. Boylece dusman
        // ARTIK ASLA "hic gelmiyor" veya "yigin halinde donuyor" olmaz.
        _spawnAngleCursor = (_spawnAngleCursor + 137.508f) % 360f;
        float fang = _spawnAngleCursor * Mathf.Deg2Rad;
        pos = player.position + new Vector3(Mathf.Cos(fang) * spawnRadius, 0f, Mathf.Sin(fang) * spawnRadius);
        // Zemin Y'si ~0 - havada spawn olmasin diye player'in Y'sine cekiyoruz.
        pos.y = player.position.y;
    }

    _recentSpawnPositions.Add(pos);
    if (_recentSpawnPositions.Count > maxRecentSpawnPositions) _recentSpawnPositions.RemoveAt(0);

    var go = PoolManager.Instance?.Get(data.PoolKey, pos, Quaternion.identity);
    go?.GetComponent<EnemyAI>()?.Initialize(player, _currentWave);
    _aliveEnemies++;
    _aliveByType[data] = _aliveByType.GetValueOrDefault(data) + 1;


  }
  void OnEnemyKilled() => _aliveEnemies = Mathf.Max(0, _aliveEnemies-1);
  void OnDestroy() => GameEvents.OnEnemyKilled.RemoveListener(OnEnemyKilled);

  // SpawnWeight'e orantili sansla secim - esit degil. Orn. [Basic(1), Bomber(0.3)]
  // icin toplam=1.3, Bomber'in secilme sansi ~%23 (0.3/1.3), Basic ~%77.
  static EnemyDataSO PickWeighted(List<EnemyDataSO> list)
  {
      float total = 0f;
      foreach (var t in list) total += Mathf.Max(0.0001f, t.SpawnWeight);

      float r = Random.value * total;
      float acc = 0f;
      foreach (var t in list)
      {
          acc += Mathf.Max(0.0001f, t.SpawnWeight);
          if (r <= acc) return t;
      }
      return list[list.Count - 1]; // guvenlik payi (float yuvarlama)
  }
   void OnDrawGizmosSelected()
   {
    if(player== null) return;
    Gizmos.color = new Color(1, 0.3f, 0, 0.3f);
    Gizmos.DrawWireSphere(player.position, spawnRadius);
   }
}