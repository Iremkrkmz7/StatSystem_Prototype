using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Oyun basladiginda karakter gokten (dropHeight kadar yukaridan) yere iner.
// Inis boyunca PlayerController ve CharacterController kapali - oyuncu WASD
// ile hareket edemez, karakter yere degene kadar kontrol acilmiyor.
[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class PlayerIntroDrop : MonoBehaviour
{
    [Header("Dusme")]
    [Tooltip("Biraz daha yukaridan dussun diye artirildi - dusus suresi (dropDuration) sabit kaldigi icin daha yuksekten daha hizli/etkileyici dusuyor")]
    [SerializeField] float dropHeight = 22f;
    [Tooltip("Sesin ~2.50 saniyesindeki 'carpma/dusme' anina denk gelsin diye 2.5 saniyeye ayarli - o anda duman/ses/Standing tetiklenir.")]
    [SerializeField] float dropDuration = 2.5f;
    [SerializeField] AnimationCurve dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Dusme baslarken calinacak sur ses (opsiyonel)")]
    [SerializeField] AudioClip fallSound;
    [Range(0f, 1f)] [SerializeField] float fallSoundVolume = 0.25f;
    [Tooltip("Dusus basladiginda Animator'da tetiklenecek Trigger parametresinin adi (Animator Controller'a 'X Bot@Falling' clip'ini state olarak edip bu isimde bir Trigger eklemen lazim). Bos birakilirsa animasyon tetiklenmez.")]
    [SerializeField] string fallAnimTrigger = "Fall";
    [Tooltip("Yere deger degmez Animator'da tetiklenecek Trigger parametresinin adi (Animator Controller'a 'X Bot@Standing' clip'ini state olarak edip bu isimde bir Trigger eklemen lazim). Bos birakilirsa animasyon tetiklenmez.")]
    [SerializeField] string landAnimTrigger = "Land";
    [Tooltip("Standing pozu, Mixamo klibinin kendi kalibrasyonu yuzunden zemine gore HAFIF asagida (ayaklar hafif yerin icinde) durabiliyor - bunu telafi etmek icin Standing SIRASINDA karakteri bu kadar yukari kaydiriyoruz. idle'a gecerken (siyah ekran maskesi sirasinda) otomatik olarak gercek zemine geri sabitlenir.")]
    [SerializeField] float standingGroundOffset = 0.15f;

    [Header("Inis Efekti")]
    [SerializeField] GameObject landingDustPrefab;
    [Tooltip("DIKKAT: Bu deger sadece boyutu degil, prefabin kendi parcacik HIZINI/MENZILINI de ayni oranda buyutur (Unity'nin Hierarchy scaling modu) - COK buyutursen (orn. 20+) parcacikler roket gibi gokyuzune firlar, 'anlik' ve 'yerden kopuk' gorunur. Kucuk tut (3-6 gibi).")]
    [SerializeField] float landingDustScale = 4f;
    [Tooltip("Inisten sonra duman TAM/YOGUN halde kac saniye kalsin (bu sure sonunda yavas yavas acilmaya/dagilmaya baslar)")]
    [SerializeField] float landingDustHoldDuration = 3.5f;
    [Tooltip("Hold suresinden sonra dumanin yavasca sonup dagilmasi icin ek sure")]
    [SerializeField] float landingDustFadeTail = 2f;
    [Tooltip("DIKKAT: hold/fadeTail sureleri sadece obje ne zaman silinecegini (Destroy timer) belirliyor - prefabin kendi parcaciklari (Particle System'in kendi startLifetime'i) o sureden ONCE sonup kaybolabiliyordu, hold suresini artirmak tek basina yetmiyordu. Bu carpan PARCACIKLARIN GERCEK omrunu (startLifetime) buyutup dumanin fiilen daha uzun sure gorunur/yerde kalmasini saglar.")]
    [SerializeField] float landingDustLifetimeMultiplier = 1.6f;
    [Tooltip("Butun parcacik simulasyonunu (hareket/genisleme HIZI ve omur azalma hizi) bu oranda yavaslatir - 1'den kucuk deger dumani daha agir/agir cekim gibi yapar. ONEMLI: onceki denemede (COK BUYUK olcek + bu ayar + manuel Simulate() fast-forward ucu birlikte) duman TAMAMEN gorunmez olmustu - simdi olcek makul (landingDustScale) ve Simulate() fast-forward KULLANILMIYOR, o yuzden guvenli. Yine de sorun cikarsa 1'e geri al.")]
    [SerializeField] float landingDustSimulationSpeed = 0.6f;
    [Tooltip("Yere carpma sesi (opsiyonel)")]
    [SerializeField] AudioClip landingSound;
    [SerializeField] float cameraShakeDuration = 0.25f;
    [SerializeField] float cameraShakeMagnitude = 0.3f;

    [Header("Kamera")]
    [Tooltip("Bos birakilirsa sahnede otomatik bulunur")]
    [SerializeField] CameraFollow cameraFollow;
    [Tooltip("Dusus BASLARKEN kameranin karaktere gore offseti - YAKIN/dar bir kadraj (referans videodaki gibi karaktere yakin baslar). Dusus boyunca buradan asagidaki 'orbit' (UZAK/genis) offsete dogru geri cekilir (reveal).")]
    [SerializeField] Vector3 dropCameraOffset = new Vector3(0f, 2.5f, -0.5f);
    [Tooltip("Kus bakisi (tepeden) acisi (derece) - 90 tam tepeden, 0 duz yatay. Dusus BOYUNCA (yakin baslangictan uzak/genis bitise kadar) bu aci SABIT kalir - referans videoda da acinin degismedigi, sadece kameranin geri cekildigi bir 'reveal' hareketi var. Aci ancak inis SONRASI (orbit bitince) normale doner.")]
    [SerializeField] float dropCameraPitch = 80f;
    [Tooltip("Dusus BITTIGINDE (yere degince) kameranin duracagi offset - UZAK/genis kadraj (referans videodaki gibi tum alani gosterir, karakter kucuk gorunur, hala KUS BAKISI). Duman/ses/Standing burada tetiklenir.")]
    [SerializeField] Vector3 revealCameraOffset = new Vector3(0f, 13f, -2f);
    [Tooltip("Orbit BASLAMADAN once kameranin kus bakisindan bu 'yandan' goruse gecis suresi")]
    [SerializeField] float toOrbitTransitionDuration = 1f;
    [Tooltip("ORBIT sirasinda kullanilan offset - YANDAN (dusuk yukseklik, genis mesafe) olmali ki karakterin YUZU/ONU gorunsun, tepeden degil. Ornegin (0,4,-8) gibi - Y, Z'den kucuk olsun.")]
    [SerializeField] Vector3 orbitCameraOffset = new Vector3(0f, 4f, -8f);
    [Tooltip("Inis sonrasi kameranin player etrafinda kac derece donecegi")]
    [SerializeField] float orbitDegrees = 90f;
    [Tooltip("Orbit hareketinin kac saniye surecegi")]
    [SerializeField] float orbitDuration = 3f;
    [Tooltip("Orbit bitince kameranin normal oyun-ici konuma/aciya donme suresi")]
    [SerializeField] float cameraReturnDuration = 1f;

    [Header("HUD")]
    [Tooltip("HUD/gameplay UI'in tamamini iceren 'UI' GameObject'i - dusus boyunca gizlenir, yere deger degmez tekrar acilir (LoadingUI/StartMenuUI'daki uiRoot ile ayni obje).")]
    [SerializeField] GameObject uiRoot;
    [Tooltip("Karakterin ustunde yuzen dunya-uzayi can bari (Player'in cocugu olan Canvas) - dusus/kus bakisi boyunca (dik kamera acisindan kenardan gorunup 'yokmus' gibi hissettirdigi icin) gizlenir, sinema bitince (HUD ile ayni anda) tekrar acilir.")]
    [SerializeField] GameObject playerHealthBarCanvas;

    [Header("Ekran Parlamasi (opsiyonel)")]
    [SerializeField] Image screenFlashImage;
    [SerializeField] float flashAlpha = 0.6f;
    [SerializeField] float flashFadeTime = 0.2f;

    [Header("Standing -> Idle Gecis Maskesi")]
    [Tooltip("TAMAMEN SIYAH, tam ekran kaplayan ayri bir Image (screenFlashImage'dan FARKLI olmali). Standing animasyonu bitip idle'a gecerken (avatar farkindan olusan kucuk 'pop') tam o anda ekran hizlica kararir, karakter/kamera arka planda normale sabitlenir, sonra tekrar acilir - boylece pop hic gorunmez.")]
    [SerializeField] Image standingPopMaskImage;
    [SerializeField] float standingPopFadeOutDuration = 0.25f;
    [SerializeField] float standingPopHoldDuration = 0.25f;
    [SerializeField] float standingPopFadeInDuration = 0.6f;
    [Tooltip("Standing state'inin normalizedTime'i bu esigi gecince (Animator'daki Standing->idle gecisinin Exit Time'indan biraz ONCE olmali ki kararma pop'tan once baslasin) maskeleme tetiklenir.")]
    [SerializeField] float standingExitThreshold = 0.85f;

    [Header("Nasil Oynanir Ipucu")]
    [Tooltip("Sinematik bitip kontrol oyuncuya gecince (SADECE ilk gercek Start'ta - restart'ta gosterilmez) bir kez gosterilecek kisa ipucu paneli. Bos birakilirsa hic gosterilmez.")]
    [SerializeField] HowToPlayOverlay howToPlayOverlay;

    PlayerController _controller;
    CharacterController _cc;
    AudioSource _audioSource;
    Animator _animator;
    bool _skipIntro;

    // EnemySpawner ilk dalgayi bu FALSE olana kadar bekliyor - yoksa dusus
    // sirasinda (player.position.y ~22'deyken) SpawnOne() hesapladigi spawn
    // noktasi da havada oluyor, NavMesh.SamplePosition (10 birim aramayla)
    // onu bulamayip o spawn'i SESSIZCE ATLIYORDU - ilk dalganin bir kismi
    // hic spawn olmadan kayboluyordu, "dusmanlar cok gec geliyor" hissi
    // buradan geliyordu. Static - sahnede baska bir PlayerIntroDrop yoksa
    // varsayilan (false) zaten dogru davranisi verir.
    public static bool IntroFallInProgress;

    void Awake()
    {
        // GameOverUI.Restart() BUNU true yapiyor - sadece GERCEK "Start"ta
        // (Editor'un skipMenusInEditor test-kolayligi dahil ilk giris) oynasin,
        // RESTART'ta (oldukten sonra "Restart" butonu) TEKRAR OYNAMASIN diye.
        // Awake, sahnedeki TUM objelerin Start()'indan ONCE calisir (Unity
        // garantisi) - bu yuzden LoadingUI/StartMenuUI kendi Start()'larinda
        // bayraklari okuyup sifirlamadan biz burada guvenle yakaliyoruz.
        _skipIntro = GameFlow.IsRestart;
        GameFlow.IsRestart = false;
        // RESTART'ta (_skipIntro=true) da karakter Y=4'ten gercek zemine
        // (~0.1) dogal gravity ile duser (asagidaki skip-branch'teki settle-wait) -
        // bu SIRADA da (asil sinematik dususteki gibi) player.position.y
        // henuz TAM oturmamis olabilir. EnemySpawner'in HER IKI durumda da
        // (restart veya normal start) sadece player GERCEKTEN yerdeyken spawn
        // etmesi icin baslangicta HER ZAMAN true - false'a sadece asagida,
        // ilgili settle-wait TAMAMLANINCA cekiliyor.
        IntroFallInProgress = true;

        _controller = GetComponent<PlayerController>();
        _cc = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponentInChildren<Animator>();
        // ONEMLI: "Apply Root Motion" acikken (FBX/Animator'un varsayilani
        // GENELDE ACIKTIR, sahnede bunun icin ayri bir override yok) Standing/
        // idle klipteki gomulu kok hareketi Animator, transform.position'i
        // BIZIM kontrolumuz DISINDA surekli degistirebilir - "karakter havada
        // kaliyor" gibi SUREKLI (tek seferlik pop degil) bir kaymaya sebep
        // olabilir. Kesin/garanti olsun diye burada acikca kapatiyoruz.
        if (_animator != null) _animator.applyRootMotion = false;

        // ONEMLI: Karakteri transform.position ile ANINDA (teleport gibi)
        // yukari tasiyoruz (dropHeight kadar). SkinnedMeshRenderer'in gorunurluk
        // sinirlari (bounds) VARSAYILAN olarak sadece render edildiginde
        // guncellenir - ani bir teleporttan sonra ESKI (zemindeki) pozisyona
        // gore hesaplanmis sinirlar kameranin yeni (yukaridaki) konumuyla
        // KESISMEDIGI icin Unity karakteri "ekran disi" sanip CIZMIYORDU
        // (zemin/harita statik oldugu icin etkilenmiyordu, sadece karakter
        // gorunmuyordu - "tepeden bakiyor ama player yok" sikayeti buradan).
        // updateWhenOffscreen=true, sinirlarin HER karede gercek kemik
        // pozisyonlarina gore yeniden hesaplanmasini saglayip bu sorunu cozer.
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        // cameraFollow'u BURADA (Awake) DEGIL, DropSequence icinde ariyoruz -
        // Awake calisirken kamera sahnede henuz olusmamis/aktif olmamis
        // olabilir (script execution order garantisi yok), bu durumda
        // FindFirstObjectByType null donup TUM kamera sekansi (reveal, orbit,
        // donus) sessizce hic calismiyordu. Coroutine, WaitUntil'den sonra
        // (birkac frame gectikten sonra) calistigi icin orada aramak cok
        // daha guvenli.
    }

    void Start()
    {
        StartCoroutine(DropSequence());
    }

    IEnumerator DropSequence()
    {
        if (_skipIntro)
        {
            // RESTART - sinematik dusus TEKRAR OYNAMASIN, ama karakter yine de
            // ham sahne pozisyonundan (orn. Y=4) gercek zemine (~0.1) dogal
            // gravity ile duser - bunu ONCEDEN (uiRoot'u hemen acip) GIZLEMEDEN
            // yapiyorduk, bu da "bosluga dusuyor" gibi GORUNUR, cirkin bir mini
            // dususe sebep oluyordu. Simdi (kontrolu KAPATMADAN, PlayerController
            // kendi gravity'siyle) yere oturana kadar HUD'u acmiyoruz/ekrani
            // karartiyoruz - oturunca aninda/gorunmeden acip gosteriyoruz.
            if (standingPopMaskImage != null)
            {
                Color c = standingPopMaskImage.color;
                c.a = 1f;
                standingPopMaskImage.color = c;
            }

            float skipSettleTimeout = 2f;
            float skipSettleElapsed = 0f;
            float skipLastY = transform.position.y;
            float skipStableFor = 0f;
            while (skipSettleElapsed < skipSettleTimeout && skipStableFor < 0.15f)
            {
                yield return null;
                skipSettleElapsed += Time.deltaTime;
                float skipDy = Mathf.Abs(transform.position.y - skipLastY);
                skipLastY = transform.position.y;
                skipStableFor = (skipDy < 0.001f) ? skipStableFor + Time.deltaTime : 0f;
            }

            // Karakter artik (restart sonrasi da) gercekten yerde - EnemySpawner
            // artik guvenle spawn edebilir.
            IntroFallInProgress = false;

            if (uiRoot != null) uiRoot.SetActive(true);
            if (playerHealthBarCanvas != null) playerHealthBarCanvas.SetActive(true);

            if (standingPopMaskImage != null)
                yield return StartCoroutine(FadeImage(standingPopMaskImage, 1f, 0f, 0.3f));

            yield break;
        }

        // Player sahne yuklenir yuklenmez (Loading ekranindayken) aktif oluyor,
        // START'a basmayi beklemiyordu. Time.timeScale=0 oldugu icin gorsel
        // dusus donuk kalip fark edilmiyordu AMA ses (timeScale'den bagimsiz
        // caldigi icin) hemen Loading sirasinda duyuluyordu. Gercek oyun
        // baslayana kadar (StartMenuUI/LoadingUI Time.timeScale'i 1 yapana
        // kadar) tum sekansi (ses dahil) bekletiyoruz.
        yield return new WaitUntil(() => Time.timeScale > 0f);

        // ONEMLI: Maskeyi BURADA (Start'a basilip gercek oyun basladiktan
        // SONRA) karartiyoruz - ONCEDEN Awake'de (Loading/Start Menu daha
        // gosterilirken) karartiyorduk, bu da maskenin YUKSEK sortingOrder'i
        // (999, her seyin ustunde) yuzunden Start Menu'nun TAMAMEN gorunmez
        // olmasina, ekranin "hic acilmiyor, karanlik" gibi durmasina sebep
        // oluyordu. Fizik "settle" bekleme suresince (Player'in ham sahne
        // pozisyonundan gercek zemine dogal duselmesi) karakter gorunur
        // olmasin diye - DropSequence icinde kamera kus-bakisi kadraja
        // oturtulduktan SONRA acariz (asagida).
        if (!_skipIntro && standingPopMaskImage != null)
        {
            Color c = standingPopMaskImage.color;
            c.a = 1f;
            standingPopMaskImage.color = c;
        }

        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();

        // ONEMLI: Zemin yuksekligini ARTIK TAHMIN ETMIYORUZ (ne sabit sayi,
        // ne Physics.Raycast, ne NavMesh.SamplePosition - ucu de bu sahnede
        // YANLIS/GUVENILMEZ cikti). Bunun yerine GERCEK fizigin (CharacterController
        // + gravity, PlayerController'in kendi kod), zaten HER ZAMAN dogru
        // sonuc verdigini gordugumuz mekanizmanin) karakteri kendi haline
        // birakip GERCEKTEN yere oturmasini bekliyoruz - Y birkac kare
        // ustuste degismeyene kadar. Ancak O ZAMAN "dogru zemin" olarak
        // kaydedip devre disi birakiyoruz.
        float settleTimeout = 2f;
        float settleElapsed = 0f;
        float lastY = transform.position.y;
        float stableFor = 0f;
        while (settleElapsed < settleTimeout && stableFor < 0.15f)
        {
            yield return null;
            settleElapsed += Time.deltaTime;
            float dy = Mathf.Abs(transform.position.y - lastY);
            lastY = transform.position.y;
            stableFor = (dy < 0.001f) ? stableFor + Time.deltaTime : 0f;
        }

        if (_controller != null) _controller.enabled = false;
        _cc.enabled = false;

        // HUD (XP bar, kill sayaci, gorevler, can barı, ability slotlari vs.)
        // dusus/kus bakisi sekansi boyunca gorunmesin - inince tekrar aciyoruz.
        if (uiRoot != null) uiRoot.SetActive(false);
        // Karakterin ustundeki dunya-uzayi can bari da ayni sekilde gizlenir -
        // dik (kus bakisi) kamera acisindan neredeyse yan/kenardan gorundugu
        // icin "yokmus" gibi hissettiriyordu.
        if (playerHealthBarCanvas != null) playerHealthBarCanvas.SetActive(false);

        // Karakter fizik ile GERCEKTEN yerlesmis - bunu "dogru zemin" olarak kullaniyoruz.
        Vector3 groundPos = transform.position;
        Vector3 startPos = groundPos + Vector3.up * dropHeight;

        // Dusus boyunca kamerayi CameraFollow'un normal Lerp mantigindan
        // COPARIP dogrudan biz yonetiyoruz. Referans video: kus bakisi ACI
        // BOYUNCA SABIT kalir, kamera sadece YAKINDAN (dropCameraOffset)
        // UZAK/genis kadraja (orbitCameraOffset) geri cekilir (reveal).
        // Normal (oyun-ici) aci/konuma donus SADECE inis+orbit sekansi
        // tamamen bittikten SONRA olur. CameraFollow SADECE pozisyonu
        // yonetiyor, rotasyona hic dokunmuyor - o yuzden "ustten bakis" icin
        // rotasyonu da BIZIM elle yonetmemiz lazim, yoksa kamera pozisyon
        // ustte olsa bile eski (yatay) aciyla baktigi icin karakteri
        // kadraja alamiyordu ("bos yer gorunmesi" sorunu buradan geliyordu).
        bool hadCamera = cameraFollow != null;
        Transform camTransform = hadCamera ? cameraFollow.transform : null;
        Vector3 originalOffset = Vector3.zero;
        Quaternion normalRotation = Quaternion.identity;
        Quaternion topDownRotation = Quaternion.identity;
        if (hadCamera)
        {
            originalOffset = cameraFollow.Offset;
            normalRotation = camTransform.rotation; // sahnede su an ayarli olan normal oyun ici aci - hedefimiz bu
            topDownRotation = Quaternion.Euler(dropCameraPitch, normalRotation.eulerAngles.y, 0f);
            cameraFollow.enabled = false; // dusus bitene kadar kamerayi biz yonetiyoruz
            camTransform.SetPositionAndRotation(startPos + dropCameraOffset, topDownRotation);
        }

        transform.position = startPos;

        // Dusus animasyonunu (X Bot@Falling) tetikle - Animator Controller'da
        // bu isimde bir Trigger parametresi ve ilgili state olmasi lazim.
        if (_animator != null && !string.IsNullOrEmpty(fallAnimTrigger))
            _animator.SetTrigger(fallAnimTrigger);

        PlayFallSound();

        // Karakter artik gokyuzunde (dropHeight), kamera kus-bakisi kadrajda -
        // ASIL sinematik dusus sahnesi hazir, maskeyi acip gosterelim.
        if (standingPopMaskImage != null)
            yield return StartCoroutine(FadeImage(standingPopMaskImage, 1f, 0f, 0.2f));

        // Dusus suresi sesin uzunluguna bagli DEGIL - sabit ve kisa (Inspector'daki
        // dropDuration). Ses (varsa) kendi tam boyuyla calmaya devam eder, dusus
        // bitip bittiginden bagimsizdir.
        float t = 0f;
        while (t < dropDuration)
        {
            // Play'e basildiktan sonraki ILK karede Time.deltaTime anormal
            // buyuk gelebiliyor (Editor baslatma gecikmesi) - bu da dusus/kamera
            // hareketinin bir kismini tek karede "atlayip" baslangicta
            // "takip etmiyormus" gibi gorunmesine sebep oluyordu. Ust sinir koyuyoruz.
            t += Mathf.Min(Time.deltaTime, 0.05f);
            float p = dropCurve.Evaluate(Mathf.Clamp01(t / dropDuration));
            Vector3 charPos = Vector3.Lerp(startPos, groundPos, p);
            transform.position = charPos;

            if (hadCamera)
            {
                // Referans videodaki "reveal" hareketi: kamera YAKIN baslayip
                // (dropCameraOffset) geri cekilip UZAK/genis kadraja
                // (revealCameraOffset) gelir - ACI (topDownRotation) BOYUNCA
                // SABIT kalir, sadece pozisyon degisir. Normal kameraya donus
                // SADECE orbit bittikten SONRA olacak.
                // Geri cekilme, karakterin dusus hizindan (dropCurve/p) BAGIMSIZ,
                // kendi "ease-in" egrisiyle ilerliyor - basta uzun sure YAKIN
                // kalip sona dogru hizlanarak uzaklassin istiyoruz (hemen
                // uzaklasip "takip etmiyor" hissi vermesin diye).
                float camP = p * p * p; // kubik ease-in: yavas basla, hizlanarak bitir
                Vector3 camOffset = Vector3.Lerp(dropCameraOffset, revealCameraOffset, camP);
                camTransform.SetPositionAndRotation(charPos + camOffset, topDownRotation);
            }

            yield return null;
        }
        // ONEMLI: Standing tetiklenmeden ONCE Y (ve tum pozisyon) KESIN
        // olarak groundPos'a esitleniyor - Standing/Land, karakter Y=0'a
        // (gercek zemine) tam ulasmadan ASLA cagrilmiyor.
        transform.position = groundPos;
        // Karakter artik havada degil - EnemySpawner ilk dalgayi buradan
        // itibaren guvenle spawn edebilir (spawn noktalari artik player'in
        // GERCEK/zemindeki Y'sine gore hesaplanacak).
        IntroFallInProgress = false;
        if (hadCamera)
        {
            // Fade tam bitmis olsun diye kesin (Lerp'in kucuk sapmalari
            // birikmesin) son degerlere sabitliyoruz. CameraFollow HALA
            // KAPALI - orbit ve donus bitene kadar kamerayi biz yonetiyoruz.
            camTransform.SetPositionAndRotation(groundPos + revealCameraOffset, topDownRotation);
        }

        // Inis animasyonunu (X Bot@Standing) SIMDI (Y kesin zeminde iken) tetikle.
        if (_animator != null && !string.IsNullOrEmpty(landAnimTrigger))
            _animator.SetTrigger(landAnimTrigger);

        // Standing pozu icin hafif yukari telafi (yukarida aciklandigi gibi) -
        // idle'a gecince (asagidaki HideStandingToIdlePop) gercek groundPos'a
        // geri sabitlenecek, o yuzden burada sadece Standing suresince gecerli.
        if (standingGroundOffset != 0f)
        {
            Vector3 sp = transform.position;
            sp.y = groundPos.y + standingGroundOffset;
            transform.position = sp;
        }

        // Standing bitip idle'a gecerken olusan kucuk "pop"u COZMEYE
        // calismak yerine (Mecanim iki farkli avatar arasinda GERCEKTEN
        // duz bir cizgide blend yapmiyor) tam o anda ekrani hizlica
        // karartip gizliyoruz - paralel calisir, kamera/orbit sekansini bloklamaz.
        if (standingPopMaskImage != null)
            StartCoroutine(HideStandingToIdlePop(groundPos));

        // Sesi ARTIK kesmiyoruz - inisten sonra (sesin ikinci yarisi boyunca)
        // calmaya devam etsin, karakter o sure icinde dumanin icinde dursun.
        CameraShake.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);
        Sfx.PlayOneShot(_audioSource, landingSound);

        if (landingDustPrefab != null)
        {
            // Zemin mesh'inin gercek yuzeyi tam Y=0 olmayabilir (biraz ustunde
            // olabilir) - duman tam Y=0'da spawn olunca zeminin altinda/icinde
            // kalip hic gorunmuyordu. Kucuk bir yukari pay veriyoruz.
            var dust = Instantiate(landingDustPrefab, groundPos + Vector3.up * 0.2f, Quaternion.identity);
            dust.transform.localScale *= landingDustScale; // ekrani kaplasin diye buyutuyoruz

            // Prefabin kendi Particle System'i loop=true geliyor - bu, dogal
            // sure (~2sn) dolunca kendini SIFIRLAYIP yeniden baslatiyor,
            // gorsel bir "kesilip tekrar baslama" (pop) sorununa sebep
            // oluyordu. loop'u KAPATIP (main.loop = false) TEK, kesintisiz
            // bir oynatim yapiyoruz - boylece sistem dogal olarak patlar,
            // biraz kalir, sonra KENDI EGRISIYLE yavasca sonup dagilir ve
            // BIR DAHA BASLAMAZ. simulationSpeed/Simulate gibi ekstra
            // manipulasyona DOKUNMUYORUZ - onlar buyuk olcekle (landingDustScale)
            // birlesince duman TAMAMEN gorunmez oluyordu.
            foreach (var ps in dust.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.loop = false;
                if (!Mathf.Approximately(landingDustLifetimeMultiplier, 1f))
                    main.startLifetime = ScaleLifetime(main.startLifetime, landingDustLifetimeMultiplier);
                if (!Mathf.Approximately(landingDustSimulationSpeed, 1f))
                    main.simulationSpeed = landingDustSimulationSpeed;
            }

            // simulationSpeed<1 iken parcaciklarin GERCEK (saniye cinsinden) omru de
            // ayni oranda uzuyor (omur azalma hizi da yavasliyor) - Destroy timer'i
            // buna gore ayarlamazsak obje, parcaciklar hala gorunurken silinebilir.
            float simSpeedSafe = Mathf.Max(landingDustSimulationSpeed, 0.01f);
            float targetDustDuration = (landingDustHoldDuration + landingDustFadeTail) * landingDustLifetimeMultiplier / simSpeedSafe;
            Destroy(dust, targetDustDuration);
        }

        if (screenFlashImage != null)
            StartCoroutine(FlashScreen());

        // KAMERA ORBIT: yere yaklasmadan (orbitCameraOffset ile ayni
        // yukseklik/mesafede kalarak) player'in etrafinda yavasca doner -
        // her karede player'a bakacak sekilde LookRotation ile yeniden
        // hesapliyoruz (offset'in Y ve yatay mesafesi SABIT kaldigi icin
        // aci/egim de otomatik sabit kalir, sadece yon degisir).
        if (hadCamera)
        {
            Vector3 lookTarget = groundPos + Vector3.up * 1.3f; // goz hizasi civari

            // ONEMLI: orbit sirasinda karakter YANDAN (yuz/on gorunecek
            // sekilde) izlensin isteniyor - kus bakisi (topDownRotation)
            // DEGIL. Bu yuzden orbit'e baslamadan once kamerayi kus
            // bakisindan (revealCameraOffset) orbit'in KENDI (dusuk/yandan)
            // offsetine ve LookRotation ile hesaplanan aciya YAVASCA
            // geciriyoruz.
            Vector3 orbitStartOffset = orbitCameraOffset; // angle=0 konumu
            Vector3 orbitStartPos = groundPos + orbitStartOffset;
            Quaternion orbitStartRot = Quaternion.LookRotation((lookTarget - orbitStartPos).normalized, Vector3.up);
            if (toOrbitTransitionDuration > 0f)
            {
                Vector3 fromPos = camTransform.position;
                Quaternion fromRot = camTransform.rotation;
                float tt2 = 0f;
                while (tt2 < toOrbitTransitionDuration)
                {
                    tt2 += Time.deltaTime;
                    float tp = Mathf.Clamp01(tt2 / toOrbitTransitionDuration);
                    camTransform.SetPositionAndRotation(
                        Vector3.Lerp(fromPos, orbitStartPos, tp),
                        Quaternion.Slerp(fromRot, orbitStartRot, tp));
                    yield return null;
                }
            }
            camTransform.SetPositionAndRotation(orbitStartPos, orbitStartRot);

            // orbitDuration 0 birakilirsa dongu hic calismaz ama asagidaki
            // "normale don" adimi HER ZAMAN calisir - yoksa cameraFollow
            // sonsuza kadar kapali kalirdi.
            if (orbitDuration > 0f)
            {
                float orbitT = 0f;
                while (orbitT < orbitDuration)
                {
                    orbitT += Time.deltaTime;
                    float angle = orbitDegrees * Mathf.Clamp01(orbitT / orbitDuration);
                    Vector3 rotatedOffset = Quaternion.Euler(0f, angle, 0f) * orbitCameraOffset;
                    Vector3 camPos = groundPos + rotatedOffset;
                    Quaternion camRot = Quaternion.LookRotation((lookTarget - camPos).normalized, Vector3.up);
                    camTransform.SetPositionAndRotation(camPos, camRot);
                    yield return null;
                }
            }

            // Orbit bitti (ya da hic yapilmadi) - simdi normal (yakin) oyun-ici konuma/aciya YAVASCA don.
            Vector3 orbitEndPos = camTransform.position;
            Quaternion orbitEndRot = camTransform.rotation;
            float rt = 0f;
            while (rt < cameraReturnDuration)
            {
                rt += Time.deltaTime;
                float rp = Mathf.Clamp01(rt / cameraReturnDuration);
                camTransform.SetPositionAndRotation(
                    Vector3.Lerp(orbitEndPos, groundPos + originalOffset, rp),
                    Quaternion.Slerp(orbitEndRot, normalRotation, rp));
                yield return null;
            }
            camTransform.SetPositionAndRotation(groundPos + originalOffset, normalRotation);
            cameraFollow.enabled = true; // normal takip devri basliyor - zaten dogru yerde oldugumuz icin sicrama olmaz
        }

        // Sinema bitti, oyun simdi basliyor - HUD acilsin, kontrol geri verilsin.
        if (uiRoot != null) uiRoot.SetActive(true);
        if (playerHealthBarCanvas != null) playerHealthBarCanvas.SetActive(true);
        _cc.enabled = true;
        if (_controller != null) _controller.enabled = true;

        // Kontrol TAM bu anda oyuncuya geciyor - "nasil oynanir" ipucunu
        // (SADECE ilk gercek Start'ta, restart'ta degil - buraya zaten
        // restart _skipIntro dalindan hic girmiyor) simdi gosterelim.
        howToPlayOverlay?.Show();
    }

    // Klibin TAMAMI (bastan sona) calsin diye dogrudan AudioSource.clip/Play
    // kullaniyoruz (Sfx.PlayOneShot ile ayni sonucu verir, sadece burada
    // suresini disaridan (fallSound.length) okuyabilmemiz gerekiyor).
    // Sfx.Volume (Ayarlar'daki Effect Sound) yine de uygulanir.
    void PlayFallSound()
    {
        if (fallSound == null) return;
        _audioSource.clip = fallSound;
        _audioSource.time = 0f;
        _audioSource.volume = fallSoundVolume * Sfx.Volume;
        _audioSource.loop = false;
        _audioSource.Play();
    }

    // Standing state'i bitip idle'a gecerken (Animator Controller'daki
    // Standing->idle gecisi, Exit Time'e ulasinca) tam o anda ekrani
    // hizlica karartir, karakterin Y'sini guvenlik icin groundPos'a
    // sabitler, kisa bir sure siyah tutar, sonra tekrar acar. Boylece
    // iki avatar arasindaki pop -Ekran siyahken oldugu icin- hic gorunmez.
    IEnumerator HideStandingToIdlePop(Vector3 groundPos)
    {
        if (_animator == null) yield break;

        // Standing state'ine girilene kadar bekle (Land tetiklenip gecis olusana kadar birkac kare surebilir).
        float waitTimeout = 3f;
        float waited = 0f;
        while (waited < waitTimeout && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Standing"))
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // Standing normalizedTime esigi gecene kadar bekle (gecis Exit Time'inden biraz once).
        float standingTimeout = 5f;
        float standingElapsed = 0f;
        while (standingElapsed < standingTimeout)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName("Standing") || info.normalizedTime % 1f >= standingExitThreshold)
                break;
            standingElapsed += Time.deltaTime;
            yield return null;
        }

        // Hizlica karart.
        yield return StartCoroutine(FadeImage(standingPopMaskImage, 0f, 1f, standingPopFadeOutDuration));

        // Ekran siyahken karakterin Y'sini kesin olarak dogru zemine sabitle -
        // TEK seferlik degil, siyah ekran boyunca HER karede (root motion
        // veya baska bir sey Y'yi tekrar kaydirmaya calisirsa bile gorunmeden
        // duzeltilsin diye).
        float holdT = 0f;
        while (holdT < standingPopHoldDuration)
        {
            Vector3 p = transform.position;
            p.y = groundPos.y;
            transform.position = p;
            holdT += Time.deltaTime;
            yield return null;
        }

        // Tekrar ac.
        yield return StartCoroutine(FadeImage(standingPopMaskImage, 1f, 0f, standingPopFadeInDuration));
    }

    // MinMaxCurve'un hangi modda oldugunu (Constant/TwoConstants/Curve) kontrol edip
    // buna gore dogru sekilde carpiyoruz - yanlis alanı carpmak (orn. Curve modundayken
    // sadece .constant'i degistirmek) hicbir etki yaratmiyordu.
    static ParticleSystem.MinMaxCurve ScaleLifetime(ParticleSystem.MinMaxCurve c, float mult)
    {
        switch (c.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return new ParticleSystem.MinMaxCurve(c.constant * mult);
            case ParticleSystemCurveMode.TwoConstants:
                return new ParticleSystem.MinMaxCurve(c.constantMin * mult, c.constantMax * mult);
            default:
                var scaled = c;
                scaled.curveMultiplier = c.curveMultiplier * mult;
                return scaled;
        }
    }

    IEnumerator FadeImage(Image img, float fromAlpha, float toAlpha, float duration)
    {
        Color c = img.color;
        c.a = fromAlpha;
        img.color = c;

        if (duration <= 0f)
        {
            c.a = toAlpha;
            img.color = c;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t / duration);
            img.color = c;
            yield return null;
        }
        c.a = toAlpha;
        img.color = c;
    }

    IEnumerator FlashScreen()
    {
        Color c = screenFlashImage.color;
        c.a = flashAlpha;
        screenFlashImage.color = c;

        float t = 0f;
        while (t < flashFadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(flashAlpha, 0f, t / flashFadeTime);
            screenFlashImage.color = c;
            yield return null;
        }
        c.a = 0f;
        screenFlashImage.color = c;
    }
}
