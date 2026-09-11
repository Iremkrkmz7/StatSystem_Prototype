using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent), typeof(SCharacterStats))]

public class EnemyAI : MonoBehaviour, IPoolable
{
     [SerializeField] EnemyDataSO data;
     [SerializeField] GameObject itemPickupPrefab;
     [SerializeField] ItemSO[] possibleDrops;
     [Range(0f, 1f)] [SerializeField] float dropChance =0.4f;

     [Header("Olum Efekti")]
     [Tooltip("Kafa mesh'i - olunce gizlenip yerine patlama efekti gelir. Bos birakilirsa efekt sadece gogus hizasinda oynar.")]
     [SerializeField] Transform headTransform;
     [Tooltip("PoolManager'daki HeadBurst prefabinin Key'i")]
     [SerializeField] string headBurstPoolKey = "HeadBurst";

     Animator _animator;


    enum State {Chase , Attack, Dead }
    State _state = State.Chase;
    Transform _target;
    int _wave;
    float _attackTimer;

    UnityEngine.AI.NavMeshAgent _agent;
    SCharacterStats _stats;

    float _destinationUpdateTimer;
    const float DestinationUpdateInterval = 0.3f; // SetDestination'ı her frame değil, bu aralıkla çağır

    // NavMesh takibi tikanirsa (restart'ta parcali/eksik bake, kopuk ada vs.)
    // dusman player'a dogru DUZ (navmesh'siz) hareket etmeye baslar - bu harita
    // acik zemin, engel yok, o yuzden duz hareket sorun degil ve dusmanin
    // "grupca durup saldirmama" bug'ini kokten cozer.
    float _noProgressTimer;
    float _lastDistToTarget = float.MaxValue;
    bool _directMove;
    const float StuckSeconds = 1.2f;      // bu kadar sure ilerleme olmazsa duz harekete gec
    const float ProgressEpsilon = 0.15f;  // her kontrolde en az bu kadar yaklasmali ki "ilerliyor" sayilsin

    // Modelin baktigi yon SADECE bu degiskende tutulur, transform.rotation'dan
    // asla geri okunmaz - boylece hareket/hedef yonu hesabi root'un anlik
    // durumundan tamamen bagimsiz kalir.
    float _facingYaw;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
       _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
       _stats = GetComponent<SCharacterStats>();
       _stats.OnDied += OnDied;

       // ONEMLI: dusman Warp/directMove ile ani sekilde tasindiginda
       // SkinnedMeshRenderer'in ONBELLEKLENMIS render sinirlari (bounds)
       // eski yerde kaliyor - Unity frustum culling dusmani "ekran disi"
       // sanip CIZMIYOR, ama collider + AI calismaya devam ediyor
       // ("gormedigim enemy tarafindan saldiriya ugruyorum"). Bu, HER
       // karede sinirlari gercek kemik pozisyonlarindan hesaplatip cozer.
       foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
           smr.updateWhenOffscreen = true;
       // NavMeshAgent'in kendi otomatik donusu bu projede guvenilir calismiyor
       // (test ettik: velocity yonunu takip etmiyor). Donusu tamamen biz yonetiyoruz.
       _agent.updateRotation = false;
       // updateUpAxis, updateRotation'dan AYRI bir ayar: kapatilmazsa agent
       // navmesh yuzey normaline gore up eksenini kendi ici guncellemesinde
       // otomatik hizalar. Bizde tum rotasyonu biz yonettigimiz icin bunu da kapatiyoruz.
       _agent.updateUpAxis = false;
       _facingYaw = transform.eulerAngles.y;
    }
    public void Initialize(Transform target ,int wave)
    {
        // ONEMLI: PoolManager, obje HALA INACTIVE iken transform.position'i
        // DOGRUDAN degistiriyor (Warp/Move degil) - NavMeshAgent bazen bunu
        // duzgun algilayip yeniden navmesh'e "baglanamiyor" (isOnNavMesh
        // yanlislikla false/gecersiz bir durumda kalabiliyor), bu da agent'in
        // SetDestination'i hic isleme almayip YERINDE DONUP KALMASINA sebep
        // oluyordu (restart sonrasi "toplu duruyor, saldirmiyor" sikayeti
        // buradan geliyordu). Warp, agent'i GERCEKTEN o pozisyondaki en yakin
        // navmesh noktasina yeniden baglayip bu sorunu cozuyor.
        if (_agent.isOnNavMesh)
        {
            _agent.Warp(transform.position); // yeniden senkronla/baglan
        }
        else if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            _agent.Warp(hit.position); // en yakin gecerli noktaya oturtup baglan
        }

        _target = target;
        _wave = wave;
        _agent.speed = data.BaseSpeed;
        // Agent, player'in fiziksel collider'ina gomulmeye calismasin diye
        // AttackRange'in biraz icinde dursun.
        _agent.stoppingDistance = data.AttackRange * 0.8f;
        _attackTimer = data.AttackCooldown;

        // Her dusmanin avoidancePriority'si farkli olsun - HEPSI ayni (50)
        // olunca Unity kalabalik simulasyonunda birbirlerine yol vermiyor,
        // ust uste biniyorlardi ("birbirine giriyor"). Farkli oncelik =
        // birbirlerinden kacinirlar.
        _agent.avoidancePriority = Random.Range(30, 71);

        // DUSMAN CANI artik TAMAMEN data-driven: EnemyDataSO.BaseHealth *
        // HealthScalePerWave^(wave-1). Eskiden can prefabin SCharacterStats'inden
        // geliyordu ve dalgayla HIC olceklenmiyordu (oyuncu gucluendikce dusmani
        // tek vurusta oldurup "zorlanmiyorum" diyordu). Artik GameBalance.xlsx /
        // balance_enemies.csv'den yonetiliyor. Havuzdan tekrar kullanimda eski
        // modifier'i temizleyip yenisini koyuyoruz.
        if (_stats != null)
        {
            _stats.RemoveModifiersFromSource(this);
            float mult = Mathf.Pow(Mathf.Max(1f, data.HealthScalePerWave), Mathf.Max(0, _wave - 1));
            float targetHp = data.BaseHealth * mult;
            float prefabBase = _stats.MaxHealth; // modifier'lar temizlendi -> prefab taban
            _stats.AddModifier(StatType.MaxHealth, StatModifier.Flat(targetHp - prefabBase, this));
            _stats.Revive(); // yeni max cana gore full doldur
        }

        // Havuzdan tekrar kullanilan dusman eski "tikandi" durumunu tasimasin.
        _noProgressTimer = 0f;
        _lastDistToTarget = float.MaxValue;
        _directMove = false;
        if (_agent.isOnNavMesh) _agent.isStopped = false;
        _state = State.Chase;
    }
    void Update()
    {
         if (_state == State.Dead || _target == null) return;

        Vector3 myPos = transform.position; myPos.y = 0;
        Vector3 targetPos = _target.position; targetPos.y = 0;
        float dist = Vector3.Distance(myPos, targetPos);

        if (dist > data.AttackRange)
        {
            _state = State.Chase;

            _destinationUpdateTimer -= Time.deltaTime;
            if (_destinationUpdateTimer <= 0f)
            {
                _destinationUpdateTimer = DestinationUpdateInterval;

                if (!_directMove)
                {
                    // Bu aralikta player'a anlamli sekilde YAKLASTIK MI?
                    if (_lastDistToTarget - dist > ProgressEpsilon)
                        _noProgressTimer = 0f;                       // ilerliyoruz
                    else
                        _noProgressTimer += DestinationUpdateInterval;

                    if (_noProgressTimer >= StuckSeconds)
                    {
                        // NavMesh takibi tikandi (parcali bake / kopuk ada /
                        // agent hic baglanamadi) - DUZ harekete gec. Bir daha
                        // menzile girene kadar bu modda kal (acik zemin, sorun degil).
                        _directMove = true;
                        if (_agent.isOnNavMesh) _agent.isStopped = true;
                    }
                    _lastDistToTarget = dist;

                    if (_agent.isOnNavMesh)
                        _agent.SetDestination(_target.position);
                    else if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var navHit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                        _agent.Warp(navHit.position);
                }
            }

            if (_directMove)
            {
                // NAVMESH'SIZ DUZ hareket - dusman player'a dogru dogrudan yurur.
                Vector3 dir = targetPos - myPos;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    Vector3 next = transform.position + dir * data.BaseSpeed * Time.deltaTime;
                    // Y'yi zemine sabitle - bu haritada COLLIDER yok, zemin
                    // yuksekligini navmesh'ten aliyoruz. Yoksa dusman sabit bir
                    // Y'de suzulup zeminin ALTINDA kalabiliyordu ("tuhaf golgeler
                    // var kendileri yok" - govde zeminin altinda, sadece golge
                    // yuzeye dusuyor). Navmesh yoksa player'in Y'sini kullan.
                    if (UnityEngine.AI.NavMesh.SamplePosition(next + Vector3.up * 3f, out var gHit, 6f, UnityEngine.AI.NavMesh.AllAreas))
                        next.y = gHit.position.y;
                    else
                        next.y = _target.position.y;
                    transform.position = next;
                    _facingYaw = Quaternion.LookRotation(dir).eulerAngles.y;
                }
            }
            else
            {
                FaceByVelocity();
            }

            _animator?.SetBool("isRunning", true);
            _animator?.SetBool("isAttacking" , false);
        }

   else
   {
    if (_agent.isOnNavMesh)
    {
        if (_agent.hasPath) _agent.ResetPath();
        _agent.isStopped = false;
    }
    // Menzile girdik - "tikandi" sayaclarini sifirla, tekrar uzaklasirsa
    // duz-hareket moduna temiz baslasin.
    _noProgressTimer = 0f;
    _lastDistToTarget = float.MaxValue;
    _directMove = false;
    _state = State.Attack;
    FaceTarget();
    _animator?.SetBool("isRunning", false);
    _animator?.SetBool("isAttacking", true);

    _attackTimer -= Time.deltaTime;
    if (_attackTimer <= 0f)
    {
        MeleeAttack();
        _attackTimer = data.AttackCooldown;
     }
    }
   }
    // Modelin GORSEL rotasyonunu her frame'in en sonunda _facingYaw'dan
    // world-space'te kuruyoruz. Model artik prefabdaki eski local rotasyon
    // yerine dogrudan bu offset'i kullaniyor. Model hareket yonunun TERSINE
    // bakiyorsa bunu 180f yap. Death'te mudahale etmiyoruz, olum animasyonu
    // kendi pozunu serbestce oynatabilsin.
    const float ModelFrontOffsetDeg = 0f;

    void LateUpdate()
    {
        if (_state == State.Dead || _animator == null) return;
        _animator.transform.rotation = Quaternion.Euler(0f, _facingYaw + ModelFrontOffsetDeg, 0f);
    }

    void MeleeAttack()
    {
        _target?.GetComponent<SCharacterStats>()?.TakeDamage(data.ScaleDamage(_wave));

        // Arka plandaki ambiyans/muzik sesini KESMEDEN (Stop() cagrilmiyor) -
        // PlayClipAtPoint kendi gecici ses kaynagini kullanir, digerlerine dokunmaz.
        if (_target != null)
            Sfx.PlayAt(data.MeleeHitSound, _target.position);

        // "isAttacking" bir BOOL - oyuncu menzilde kaldigi surece surekli true
        // gonderildigi icin Mecanim sadece ILK gecistte (false->true) animasyonu
        // oynatiyor, sonraki vuruslar da bool zaten true oldugundan animasyon
        // hic yeniden baslamiyordu (vurus sadece bir kere gorunuyordu). Her
        // vurusta "Attack" state'ini basindan zorla yeniden baslatiyoruz.
        _animator?.Play("Attack", 0, 0f);
    }
    void FaceTarget()
    {
        if(_target== null) return;
        Vector3 dir = (_target.position - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            TurnTowards(dir);
    }

    // Chase durumunda: NavMeshAgent'in gercek hareket yonune (velocity) gore doner.
    // Boylece "nereye gidiyorsa oraya donsun" tam olarak istenen davranisi verir.
    // Velocity cok kucukse (henuz hizlanmaya baslamamis/durmus) hedefe dogru bakmayi tercih eder.
    void FaceByVelocity()
    {
        Vector3 vel = _agent.velocity;
        vel.y = 0;
        Vector3 dir = vel.sqrMagnitude > 0.01f ? vel : (_target.position - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            TurnTowards(dir);
    }

    // Hedef yawi transform'dan degil, sadece dir vektorunden hesaplar ve
    // _facingYaw'i buna dogru yumusatir. transform.rotation hicbir zaman
    // okunmaz.
    void TurnTowards(Vector3 dir)
    {
        float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _facingYaw = Mathf.LerpAngle(_facingYaw, targetYaw, 10f * Time.deltaTime);
    }
    void OnDied()
    {
        if(_state == State.Dead) return;
        _state = State.Dead;
        // Olum animasyonunu HEMEN tetiklemiyoruz - once dusman kisa bir sure
        // (hala ayaktaymis gibi) donuk kalip patlama oynuyor, sonra yere
        // yigilma animasyonuna geciyor. Boylece "once patlama, sonra dusme"
        // sirasi olusuyor; ikisi ayni anda olursa patlama zaten cokmekte olan
        // bir govdenin ustunde/yaninda oynadigi icin yerde gibi hissettiriyordu.
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        // NavMeshAgent durdurulsa bile Animator'in kendi Root Motion'i hala
        // aktifse, o an oynayan animasyonun (Run/Walk) hareket verisiyle govde
        // kaymaya devam ediyordu - patlama da bu yuzden kafadan uzaklasiyordu.
        // Olum aninda kokten kapatiyoruz, govde tamamen sabitlensin.
        if (_animator != null) _animator.applyRootMotion = false;

        // KAFA PATLAMA EFEKTI: artik HER olumde oynuyor.
        Vector3 burstPos = headTransform != null ? headTransform.position : transform.position + Vector3.up * 1.5f;
        burstPos += Vector3.up * data.HeadBurstHeightOffset; // efekt kafanin biraz ustunde gorunsun
        if (headTransform != null) headTransform.gameObject.SetActive(false);
        // Root motion artik kapali oldugu icin govde olum sirasinda yerinde
        // kalir - patlamanin govdeyi "takip etmesine" gerek kalmadi, sabit
        // spawn pozisyonu (dogru kafa noktasi) yeterli ve guvenilir.
        PoolManager.Instance?.Get(headBurstPoolKey, burstPos, Quaternion.identity);

        Sfx.PlayAt(data.DeathSound, transform.position);

        // BOMBER TIPI DUSMANLAR: olunce yakinindaki herkese (oyuncu + diger
        // dusmanlar) patlama hasari verir. Normal dusmanlarda ExplodesOnDeath
        // false oldugu icin bu blok hicbir sey yapmaz.
        if (data.ExplodesOnDeath)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, data.ExplosionRadius);
            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                if ((col.CompareTag("Player") || col.CompareTag("Enemy")) && col.TryGetComponent<SCharacterStats>(out var hitStats))
                    hitStats.TakeDamage(data.ExplosionDamage);
            }

            // Kucuk kafa patlamasindan bagimsiz, cevreyi saran ayri bir gorsel -
            // yerde, hasar yaricapiyla orantili buyuklukte yanip birkac saniye sonra kayboluyor.
            if (data.DeathExplosionVFX != null)
            {
                var groundFx = Instantiate(data.DeathExplosionVFX, transform.position, Quaternion.identity);
                groundFx.transform.localScale *= Mathf.Max(1f, data.ExplosionRadius / 1.75f);
                // Yerdeki alev sadece gorsel degil - icinde duran oyuncuya sure boyunca
                // periyodik hasar vermeye devam etsin (tek seferlik patlama hasarindan bagimsiz).
                // ONEMLI: VFX_GroundFire_Circle prefabinin UZERINDE bu component hic yoktu
                // (sadece GetComponent kullaniliyordu, bulamayinca sessizce hicbir sey
                // yapmiyordu) - bu yuzden ne periyodik hasar ne de FireHitSound hic
                // calismiyordu. AddComponent ile yoksa runtime'da ekliyoruz, garanti calisir.
                var fireZone = groundFx.GetComponent<GroundFireDamageZone>();
                if (fireZone == null) fireZone = groundFx.AddComponent<GroundFireDamageZone>();
                fireZone.Configure(data.ExplosionRadius, data.ExplosionDamage * 0.3f, 0.5f, data.FireHitSound);
                if (data.FireLoopSound != null)
                {
                    var fireAudio = groundFx.AddComponent<AudioSource>();
                    fireAudio.clip = data.FireLoopSound;
                    fireAudio.loop = true;
                    fireAudio.spatialBlend = 1f; // 3D - kaynaga yaklastikca duyulsun
                    fireAudio.Play();
                }
                Destroy(groundFx, 4f);
            }
        }

        // Vurusun agirligini hissettirmek icin kisa ekran sarsintisi + hit-stop.
        // AoE ile ayni anda cok dusman olebilir, ikisi de merkezi singleton
        // uzerinden yonetildigi icin cakismaz.
        CameraShake.Instance?.Shake(0.15f, 0.2f);
        HitStop.Instance?.Trigger(0.05f, 0.05f);

        GameEvents.OnEnemyKilled.Raise();
        EnemySpawner.Instance?.NotifyEnemyDied(data); // MaxAlive/tip bazli sayaci azalt
        AchievementManager.Instance?.Increment("EnemiesKilled");

        // DALGAYA GÖRE XP HESAPLA VE TEK SEFER EKLE.
        // Eskiden burada XP iki kere ekleniyordu: bir kere data.ScoreValue ile,
        // sonra (kullanilmayan/olu ExperienceManager sistemi hala sahnede
        // var diye) ayni miktar TEKRAR ekleniyordu - bu da beklenenden 2 kat
        // hizli level atlanmasina, ust uste gereksiz level-up ekranlarina
        // sebep oluyordu. Artik tek kaynaktan (xpReward) tek sefer ekleniyor.
        int xpReward = data.ScaleXP(_wave);
        XPSystem.Instance?.AddXP(xpReward);

        if (data.FloatingTextPrefab != null)
        {
            // _target (player) null olsa bile (edge-case) yazi yine cikar -
            // eskiden target null olunca HICBIR "+XP / +KILL" yazisi
            // gozukmuyordu. Target varsa onun ustunde, yoksa dusmanin
            // kendi olum yerinde gosteriyoruz.
            Transform anchor = _target != null ? _target : null;
            Vector3 basePos = _target != null ? _target.position : transform.position;

            GameObject xpText = Instantiate(data.FloatingTextPrefab, basePos + Vector3.up * 2f, Quaternion.identity);
            xpText.GetComponent<FloatingText>()?.SetText($"+{xpReward} XP", new Color(0f, 0.8f, 1f), anchor);

            Vector3 killOffset = new Vector3(0.8f, 2.5f, 0f); // Sağ çapraz konumu
            GameObject killText = Instantiate(data.FloatingTextPrefab, basePos + killOffset, Quaternion.identity);
            killText.GetComponent<FloatingText>()?.SetText("+1 KILL", Color.yellow, anchor);
        }
    

        if (itemPickupPrefab != null && possibleDrops.Length > 0 && Random.value < dropChance)
        {
            var item = possibleDrops[Random.Range(0, possibleDrops.Length)];
            var go = Instantiate(item.PickupPrefab != null ? item.PickupPrefab : itemPickupPrefab,
            transform.position + Vector3.up * 0.5f, Quaternion.identity);

            var pickup = go.GetComponent<ItemPickup>();
            pickup?.SetItem(item);
        }
        
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Patlama dusman hala ayaktayken bir sure oynasin (kisa bir "donma" ani),
        // sonra yere yigilma animasyonuna gecsin.
        yield return new WaitForSeconds(data.DeathFreezeDuration);
        _animator?.SetTrigger("Death");

        // Govdeyi patlama efekti bitene kadar ekranda tutuyoruz, yoksa govde
        // hemen kaybolup patlama "havada asili/yerde" gibi gorunuyordu.
        yield return new WaitForSeconds(1.8f);
        PoolManager.Instance?.Return(data.PoolKey, gameObject);
    }

    // Can, esik degerin altina dusunce dusman surekli yanmaya baslar; can
    // tekrar esigin ustune cikarsa (heal/regen) alev soner. Olum aninda
    // OnDied zaten bunu temizliyor.
    public void OnSpawnFromPool()
    {
        _state = State.Chase;
        _attackTimer = data?.AttackCooldown ?? 1f;
        _facingYaw = transform.eulerAngles.y;
        _stats?.Revive();
        if (headTransform != null) headTransform.gameObject.SetActive(true); // onceki olumden kalma gizli kafayi geri ac
        _animator?.SetTrigger("Spawn");

        StartCoroutine(EnableAgentDelay());
    }

    IEnumerator EnableAgentDelay()
    {
        yield return null;
        if (_agent) 
        {
            _agent.enabled = true;

            if (!_agent.isOnNavMesh)
            {
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _agent.Warp(hit.position);
                }
            }

            // ÖNEMLİ: updateRotation'i artık NavMeshAgent'a bırakmıyoruz - biz FaceByVelocity/
            // FaceTarget ile tam kontrol ediyoruz, agent'ın kendi otomatik dönüşü güvenilmez
            // çıktı. Her spawn'da açıkça false'a zorluyoruz, geçmişten bağımsız garanti olsun.
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }
    }

    public void OnReturnToPool()
    {
        if(_agent) _agent.enabled = false;
        StopAllCoroutines();
        _state = State.Chase;
        _directMove = false;
    }

    // Sahne yeniden yuklendiginde (restart) - PoolManager DontDestroyOnLoad
    // oldugu icin O AN CANLI olan dusmanlar (havuza donmemis olanlar)
    // reload'dan SAG CIKIP yeni oyunda player'in dibinde takili "zombi"
    // olarak kaliyordu. Yeni EnemySpawner basladiginda hepsini bununla
    // temizliyoruz - olum efekti oynatmadan sessizce havuza geri koyar.
    public void ForceDespawn()
    {
        StopAllCoroutines();
        _state = State.Dead;
        if (data != null) PoolManager.Instance?.Return(data.PoolKey, gameObject);
        else gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if(data== null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.AttackRange);
    }
}