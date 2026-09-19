# Wizard Survival (StatSystem_Prototype)

**🇹🇷 Türkçe** · [🇬🇧 English](README.md)

Üstten görünümlü, dalga bazlı hayatta kalma (survivor-like) bir Unity oyunu.
Prosedürel olarak üretilen sonsuz bir dünyada, giderek güçlenen düşman
dalgalarına karşı hayatta kalırsın; XP toplayıp seviye atlar, her seviyede üç
yükseltmeden birini seçer, yetenekler kullanır, görev ve başarımları
tamamlarsın. İlerlemen kalıcı olarak kaydedilir ve sonraki oyun kaldığın
yerden devam eder.

**Unity 2022.3 · URP · C#**

**▶ Tarayıcıda oyna: [Magic Survival — itch.io](https://7iremkrkmz.itch.io/magic-survival)**

---

## Bu depo ne içeriyor?

Bu depo **yalnızca kendi yazdığım kod ve tasarladığım veriyi** içerir.
Oyunun tam sürümünde kullanılan ücretli/lisanslı Unity Asset Store paketleri
(3B modeller, VFX, ikon setleri, animasyonlar, müzikler) ve ana sahne dosyası
**dahil değildir** — bu paketlerin lisansları ham dosyaların yeniden
dağıtılmasına izin vermiyor.

Dolayısıyla depoyu klonlayıp Unity'de açarsan sahne/prefab referansları eksik
çıkar. **Amaç kodu incelemek**; oynamak için yukarıdaki itch.io linkine bak.

---

## Oynanış

| Girdi | Aksiyon |
|---|---|
| **WASD** | Hareket |
| **Fare** | Nişan alma (karakter imlece döner) |
| **Sol tık (basılı)** | Ateş etme |
| **Q / yetenek tuşları** | Dash, Shield, AoE, Heal |
| **Ability ikonları** | Tıklayarak da kullanılabilir |

**Döngü:** Dalga başlar → düşmanlar spawn olur → öldür, XP ve elmas topla →
seviye atla, üç yükseltmeden birini seç → dalga biter, kısa mola → bir sonraki
dalga daha kalabalık ve daha güçlü. Ölünce (ya da menüye çıkınca) en yüksek
dalga ve seviyen kaydedilir; yeni oyun oradan başlar.

---

## Sistemler

### Stat sistemi (projenin çekirdeği)

Modifier tabanlı, klasik RPG mantığında bir istatistik sistemi — projenin adı
(`StatSystem_Prototype`) buradan geliyor.

- **`CharacterStat`** — bir istatistiğin taban değeri + üzerine binen modifier
  listesi. Değer **cache'lenir**, yalnızca liste ya da taban değiştiğinde
  (`_isDirty`) yeniden hesaplanır.
- **`StatModifier`** — üç tip: `Flat` (sabit ekleme), `PercentAdd` (yüzdeler
  toplanır), `PercentMult` (yüzdeler çarpılır). Uygulama sırası `Order` ile
  garanti altında: önce Flat, sonra PercentAdd, en son PercentMult.
- **Kaynak takibi (`Source`)** — her modifier kendini ekleyen nesneyi tutar,
  böylece `RemoveModifiersFromSource(this)` ile o kaynağın tüm etkileri temiz
  bir şekilde geri alınabilir. Havuzdan tekrar kullanılan düşmanlarda buff
  birikmesini önleyen mekanizma budur.
- **`StatType`** — MaxHealth, Speed, Damage, Defense, AttackSpeed, CritChance,
  PickupRadius.
- **`SCharacterStats`** — bu sistemi taşıyan MonoBehaviour. Hem oyuncuda hem
  düşmanlarda kullanılır; can, hasar alma/verme, iyileşme, ölüm ve seviye
  atlama tepkilerini yönetir.

### XP, seviye ve yükseltme seçimi

- **`XPSystem`** — XP biriktirme, seviye eşiği (`baseXPRequired × xpMultiplier^level`),
  seviye atlama olayları. `SetLevelSilently()` ile UI'ı tetiklemeden seviye
  atanabilir (kayıttan devam ederken kullanılır).
- **`LevelUpUI`** — seviye atlayınca oyun duraklar ve **üç rastgele yükseltme
  kartı** sunulur (Vampire Survivors tarzı). Seçim için **süre sınırı** vardır;
  geri sayım çubuğu, sesin süresiyle birebir senkronize çalışır ve dolduğunda
  sarıdan kırmızıya geçer.
- **`UpgradeOption`** — yükseltme tanımı (hangi stat, hangi modifier tipi, ne
  kadar). Seçilen yükseltme kalıcı bir modifier olarak eklenir.
- Bunun **üzerine** her seviyede otomatik can/hasar artışı da uygulanır
  (`healthIncreasePerLevel`, `damageIncreasePerLevel`).

### Düşman sistemi ve dalgalar

**`EnemyDataSO`** ile her düşman tipi veri olarak tanımlanır:

| Alan | İşlevi |
|---|---|
| `MinWave` | Bu dalgadan önce hiç çıkmaz |
| `MinPlayerLevel` | Oyuncu bu seviyeye gelmeden çıkmaz (MinWave'den bağımsız, ikisi de sağlanmalı) |
| `SpawnWeight` | Ağırlıklı rastgele seçim — nadir tipler için düşük değer |
| `MaxAlive` | Aynı anda sahnede bu tipten en fazla kaç tane olabilir |
| `BaseHealth/Speed/Damage/AttackRange/AttackCooldown` | Temel savaş statları |
| `HealthScalePerWave`, `DamageScalePerWave` | Dalga başına üstel güçlenme |
| `BaseXP`, `XPScalePerWave`, `ScoreValue` | Ödüller |
| `ExplodesOnDeath` + yarıçap/hasar/VFX | Bomber tipi düşmanlar için ölüm patlaması |

**`EnemySpawner`** dalga akışını yönetir: dalga başına düşman sayısı
(`baseEnemyCount + countIncrement × dalga`, `maxWaveEnemyCount` ile sınırlı),
uygunluk filtresi (MinWave / MinPlayerLevel / MaxAlive), ağırlıklı tip seçimi
ve spawn konumu üretimi.

**Spawn dağılımı** — düşmanların tek bir noktada yığılmaması için:
altın açı (137.5°) ile dönen bir açı imleci, ±25° sapma, `spawnRadius`'un
%85–115'i arası mesafe, son 20 spawn noktasına minimum uzaklık kontrolü ve
NavMesh'e oturtma. Hiçbir aday geçerli çıkmazsa açısal olarak dağıtılmış ham
bir konuma düşülür.

### Düşman yapay zekâsı

**`EnemyAI`** — NavMesh tabanlı takip ve saldırı, üzerine birkaç katman
dayanıklılık:

- **Takılma tespiti** — hedefe olan mesafe `StuckSeconds` boyunca anlamlı
  şekilde azalmıyorsa düşman "takılmış" sayılır.
- **Doğrudan hareket yedeği** — takıldığında NavMesh bırakılıp hedefe doğrudan
  ilerlenir; her adımda zemin yüksekliği `NavMesh.SamplePosition` ile
  örneklenerek Y düzeltilir (aksi halde düşmanlar zemine gömülüp yalnızca
  gölgeleri görünüyordu).
- **Havuzdan dönüşte yeniden bağlanma** — `NavMeshAgent.Warp()` ile agent'ın
  navmesh'e gerçekten oturması garanti edilir.
- **Kalabalık davranışı** — her düşmana rastgele `avoidancePriority` (30–70)
  verilir; hepsi aynı öncelikte olduğunda Unity'nin kalabalık simülasyonu
  birbirlerine yol vermiyor ve düşmanlar iç içe geçiyordu.
- **Görünürlük** — tüm `SkinnedMeshRenderer`'larda `updateWhenOffscreen = true`;
  ani konum değişiminden sonra bayatlayan render sınırları yüzünden düşmanlar
  görünmez hâlde saldırabiliyordu.

### Prosedürel dünya

- **`ChunkManager`** — oyuncunun etrafında sonsuz chunk yükleme/boşaltma
  (`renderDistance`, `chunkSize`), seed tabanlı deterministik üretim, kare
  başına iş sınırı (`maxOpsPerFrame`) ile takılmasız yükleme.
- **`WorldChunk`** — her chunk kendi proplarını ve zemin dekorlarını yerleştirir.
  El ile konumlandırılmış spawn noktaları, chunk boyutu değiştiğinde
  `referenceChunkSize`'a göre orantılı ölçeklenir; spawn noktası tanımlı
  değilse yoğunluk tabanlı rastgele yerleştirmeye düşer.
- **Çalışma zamanı NavMesh** — dünya dinamik üretildiği için NavMesh de
  çalışma zamanında asenkron olarak pişirilir; oyunun ilk saniyelerinde
  kademeli yeniden bake'ler yapılır.

### Nesne havuzu (Object pooling)

**`PoolManager`** — mermiler, düşmanlar, efektler ve chunk'lar için anahtar
bazlı havuzlama. `IPoolable` arayüzü ile nesneler havuza dönerken kendilerini
sıfırlar. Havuz sahne yeniden yüklemelerinde yaşar (`DontDestroyOnLoad`);
`AutoReturnToPool` ile süreli efektler kendiliğinden geri döner.

### Yetenekler

**`AbilitySO`** ile veri olarak tanımlanır: tip (**Dash / Shield / AoE / Heal**),
aktivasyon tuşu, bekleme süresi, etki değeri ve süresi, VFX prefab'ı.
**`AbilityController`** bekleme sürelerini yönetir; bekleme süresi oyuncu
seviyesiyle birlikte kısalır (`Cooldown / (1 + abilityCDLevelFactor × level)`).
**`AbilitySlotUI`** her slotu, bekleme süresince dolan bir maske animasyonuyla
gösterir.

### Silah ve mermiler

**`WeaponSO`** (hasar, atış hızı, mermi prefab'ı) + **`WeaponHandler`** (atış
zamanlaması — oyuncunun `AttackSpeed` statı atış aralığını böler) +
**`Projectile`** (havuzlanmış mermi, çarpma tespiti, `HitEffect`).

### Envanter, eşyalar ve elmaslar

**`ItemSO`** tipleri: HealthPack, WeaponPart, ArmorPart, BuffItem, Collectible,
Diamond, AoEItem. **`ItemPickup`** yerdeki eşyaları, **`InventorySystem`**
toplama ve kullanma mantığını yönetir. Elmaslar iki ayrı sayaçta tutulur:
tur içi sayaç (tur sonu ekranı ve rekor için) ve **kalıcı toplam** (toplandığı
anda kaydedilir, oyun kapansa bile kaybolmaz).

### Görevler ve başarımlar

- **`QuestSO` / `QuestManager`** — beş görev tipi: `KillEnemies`,
  `CollectDiamonds`, `SurviveSeconds`, `KillWithoutDamage`, `MultiKill`.
  İlerleme olay tabanlı takip edilir, tamamlanınca XP ve elmas ödülü verilir.
- **`AchievementSO` / `AchievementManager`** — başarımlar kalıcı olarak
  kaydedilir; yeni oyunda daha önce açılanlar sessizce geri yüklenir
  (bildirim tekrar gösterilmez), yeni açılanlar bildirim paneliyle duyurulur.

### Kayıt sistemi ve "kaldığın yerden devam"

**`SaveSystem`** (PlayerPrefs tabanlı) şunları kalıcı tutar: en yüksek dalga,
en yüksek oyuncu seviyesi, en yüksek kill/elmas rekoru, biriken toplam elmas,
açılan başarımlar, "nasıl oynanır" ekranının gösterilip gösterilmediği.

**Önemli tasarım kararı:** en yüksek dalga ile en yüksek oyuncu seviyesi
**bağımsız** olarak saklanır. Dalga sayısı hayatta kalmayla, seviye ise XP ile
ilerler ve ikisi aynı hızda gitmez — seviyeyi dalgadan türetmek "11. seviyede
öldüm, 14'te başladım" gibi tutarsızlıklara yol açıyordu.

Yeni bir tur başlarken kayıtlı dalga ve seviye geri yüklenir; aradaki seviye
farkının getireceği can/hasar artışı **tek seferde, sessizce** uygulanır
(`ApplyCatchUpLevels`) — seviye atlama ekranı arka arkaya açılmaz.

Kayıt hem ölünce hem de **ölmeden menüye çıkınca** yapılır: sistem bir "oturum
kaydı" değil rekor kaydı olduğu için (değerler yalnızca yükselir), ulaşılan
dalgayı kaydetmemek hatalı bir davranış gibi hissettiriyordu.

### Giriş sinematiği

**`PlayerIntroDrop`** — karakterin gökten düşüşü, kamera kuş bakışı açılış,
yörünge (orbit) hareketi ve oyun içi kameraya yumuşak geçiş. İniş tozu, ses,
kamera sarsıntısı eşlik eder. Restart'ta sinematik atlanır ama karakterin
zemine oturması yine maskelenir. Kontrol tam olarak oyuncuya geçtiği an
"nasıl oynanır" paneli gösterilir (ömür boyu yalnızca bir kez) ve düşman
spawn sayacı ancak o panel kapandıktan sonra başlar.

### Arayüz

`HUDController` (can, XP, dalga, kill sayacı) · `HealthBar` (dünya uzayında
düşman can barları) · `DamageVignette` (darbe alınca kırmızı flaş + düşük canda
kalıcı kenar vurgusu) · `MultiKillUI` (Double/Triple/Quadra/Penta Kill
banner'ı + bonus XP) · `FloatingText` (hasar, XP, iyileşme yazıları) ·
`QuestUI` · `InventoryUI` · `AchievementNotificationUI` · `LevelUpUI` ·
`PauseUI` · `GameOverUI` · `SettingsUI` (ses ayarları, PlayerPrefs ile kalıcı) ·
`LoadingUI` / `StartMenuUI` · `HowToPlayOverlay` · `GameTimer` ·
`GlobalButtonSfx` (tüm butonlara otomatik tık sesi).

### Oyun hissi (game feel)

`CameraShake` (sönümlenen sarsıntı) · `HitStop` (vuruş anında kısa zaman
yavaşlatma) · `EnemyFlash` (hasar alınca beyaz flaş) · `EnemyShake` ·
`DeathExplosion` (bomber ölümünde alan hasarı + VFX) ·
`GroundFireDamageZone` (patlama sonrası yerde kalan hasar alanı) ·
`PlayerLevelUpEffect` · `TwinkleEffect`.

### Olay sistemi

`GameEvent` / `TypedGameEvent` / `GameEventListener` — ScriptableObject
tabanlı, gevşek bağlı olay kanalları. Sistemler birbirini doğrudan referans
almadan haberleşir (örn. düşman ölümünü görev, başarım ve multi-kill
sistemlerinin hepsi dinler).

---

## Veri odaklı denge (balance) hattı

Denge değerleri koda gömülü değil; Excel'den oyuna akan bir hat üzerinden
yönetiliyor:

```
GameBalance.xlsx   →   balance_global.csv    →   BalanceConfig.asset
   (elle düzenle)       balance_enemies.csv       EnemyDataSO asset'leri
                          (CSV olarak kaydet)    (Unity menüsünden import)
```

- **`GameBalance.xlsx`** — 8 sayfalık çalışma kitabı: Config, XP & Levels,
  Player Progression, Enemies, Waves, Abilities, Economy & Meta, Balance
  Issues. Formüllerle seviye/dalga eğrilerini önden görebiliyorsun.
- **`BalanceConfig`** — `Resources` altında tek bir ScriptableObject; XP eğrisi,
  seviye başına can/hasar artışı, dalga boyutları, spawn yarıçapı/aralığı,
  dalga molası, yetenek bekleme çarpanı gibi tüm global ayarların **tek
  doğruluk kaynağı**. `EnemySpawner`, `XPSystem`, `SCharacterStats` ve
  `AbilityController` değerleri buradan okur.
- **`BalanceImporter`** (Editor) — `Tools ▸ Balance` menüsü:
  `Import ALL from CSV`, `Import Global only`, `Import Enemies only`,
  `Export CURRENT values to CSV`. Reflection ile alanları isimlerine göre
  eşleyip yazar, bilinmeyen alanları sessizce atlar.

Sonuç: dengeyi değiştirmek için kod değiştirmek ya da Inspector'da tek tek
alan aramak gerekmiyor — tabloyu düzenle, CSV kaydet, menüden import et.

---

## Depo yapısı

```
Assets/
  Scripts/
    Core/
      Ability/       Yetenek tanımları ve kontrolcüsü
      Achievement/   Başarım sistemi
      Events/        ScriptableObject tabanlı olay kanalları
      Inventory/     Eşya, toplama, envanter
      Pool/          Nesne havuzu
      Quest/         Görev sistemi
      Stats/         Stat/modifier çekirdeği (projenin kalbi)
      BalanceConfig.cs, SaveSystem.cs, XPSystem.cs, GameFlow.cs, Sfx.cs
    Enemy/           Düşman AI, spawner, veri, ölüm efektleri
    Player/          Kontrol, savaş, kamera, giriş sinematiği
    Weapon/          Silah, mermi, vuruş efekti
    World/           Chunk tabanlı prosedürel dünya
    UI/              Tüm arayüz ve oyun hissi katmanı
  Editor/
    BalanceImporter.cs   Excel/CSV → oyun verisi import aracı
  ScriptableObjects/     Düşman, yetenek, görev, eşya, başarım tanımları
  Resources/
    BalanceConfig.asset  Merkezi denge ayarları
GameBalance.xlsx
balance_global.csv, balance_enemies.csv
```

---

## Yol boyunca çözülen kayda değer teknik sorunlar

Bu bölüm, geliştirme sırasında karşılaşılan ve teşhisi kolay olmayan
problemleri belgeliyor:

- **NavMesh çalışma zamanında sessizce çalışmıyordu.**
  `NavMeshSurface.UpdateNavMesh()` geometriyi `NavMeshData` nesnesine pişirir
  ama onu çalışma zamanı navigasyon sistemine **kaydetmez**. `OnEnable()`
  içindeki otomatik `AddData()` çağrısı, o anda `navMeshData` henüz `null`
  olduğu için hiçbir şey kaydetmiyordu. Sonuç: hata vermeyen, ama tamamen
  işlevsiz bir navmesh — düşmanlar hareket etmiyordu. Çözüm: her bake sonrası
  açıkça `RemoveData()` + `AddData()`.

- **Sahne yeniden yüklemesinde havuz bozuluyordu.**
  Havuza dönen nesneler kalıcı havuz nesnesinin altına geri taşınmadığı için,
  sahneyle birlikte yok ediliyor ve havuz ölü referanslar tutuyordu. Sonraki
  `Get()` çağrısı `MissingReferenceException` atıp chunk üretim döngüsünü
  ortasında kesiyordu — dünyada delikler oluşuyordu.

- **Görünmez ama saldıran düşmanlar.**
  Ani konum değişiminden (Warp/teleport) sonra `SkinnedMeshRenderer`'ın
  önbellekteki render sınırları bayatlıyor, Unity'nin görüş alanı elemesi mesh'i
  çizmeyi atlıyordu; fizik ve AI çalışmaya devam ettiği için düşman görünmeden
  saldırabiliyordu.

- **Sonsuz kamera sarsıntısı.** Sarsıntı döngüsünde sayaç hiç artırılmıyordu
  (`while (elapsed < duration)` içinde `elapsed` sabitti). Hata, düşmanlar
  gerçekten vurmaya başlayana kadar görünür olmadı.

- **Coroutine'lerde değişken çakışması.** `yield return` içeren metotlar durum
  makinesi sınıfına derlendiği için, birbiriyle hiç kesişmeyen bloklardaki
  aynı isimli yerel değişkenler bile `CS0136` hatası veriyor.

- **"Beyaz ekran" gizemi.** Unity, texture'ı boş bir `RawImage`'ı şeffaf değil
  **opak beyaz** olarak çizer. Tam ekran olan bu boş katman, altındaki menü
  arka planını tamamen örtüyordu.

- **WebGL'de video oynatılamaz.** Unity'nin `VideoPlayer` bileşeni WebGL
  derlemelerinde desteklenmiyor; Editor'de çalışıp build'de siyah kalıyordu.
  Menü arka planı her platformda tutarlı olsun diye statik görsele çevrildi.

- **Çözünürlük uyuşmazlığı.** Arayüz "Constant Pixel Size" ile mutlak piksel
  koordinatlarında yerleştirildiği için, tasarım çözünürlüğünden farklı bir
  build çözünürlüğünde elemanlar ekran dışına taşıyordu.

- **VFX Graph tarayıcıda çalışmıyor — ve oyunu komple çökertiyordu.**
  Oyuncunun mermisi bir VFX Graph efektiydi. VFX Graph "compute shader"
  gerektiriyor, WebGL 2.0'da bu yok; tarayıcı build'i havuzdaki her mermi için
  `Invalid VFX Particle System` yazıp açılışta `RuntimeError: memory access out
  of bounds` ile ölüyordu. İlginç ayrıntı: aynı build bir makinede efekti sessizce
  atlayıp sorunsuz çalışıyor, diğerinde çöküyordu — davranış ekran kartına bağlı
  olduğu için ilk bakışta "bende olmuyor" gibi görünüyordu. Her platformda
  çizilen additive bir mesh ile değiştirildi.

- **Tarayıcıda sesler geç geliyordu.** Tüm ses dosyalarında *Preload Audio Data*
  kapalıydı; yani her klip ilk çalındığı anda çözülüyordu. Yerel diskte fark
  edilmiyor, tarayıcıda gecikme olarak duyuluyor, üstelik savaşın ortasında
  yapıldığı için takılmaya da sebep oluyordu. Önceden yükleme, bu işi Loading
  ekranına taşıyor.

- **Düşman vuruşları savuruştan bir saniye sonra iniyordu.** Saldırı animasyonu
  düşman menzile girer girmez başlıyor, ama hasar ve vuruş sesi `AttackCooldown`
  sayacının dolmasını bekliyordu — ilk savuruş sessiz ve hasarsız kalıyor,
  sonraki vuruşlarda da ses temas anında değil savuruşun *başında* çalıyordu.
  Artık hasar ve ses animasyonun temas anına zamanlanıyor; oyuncu savuruş
  sırasında menzilden kaçabildiyse vuruş boşa gidiyor.

- **NavMesh'in yeniden hesaplanması tarayıcıyı donduruyordu.** Her chunk
  yüklenmesi/kaldırılması NavMesh'i kirli işaretliyor, bu da 300×300 m'lik bir
  alanın 0.17 m çözünürlükte (yaklaşık 3.2 milyon hücre) baştan hesaplanmasını
  tetikliyordu. WebGL build'lerinde iş parçacığı desteği olmadığı için bu hesap
  ana döngüde yapılıyor ve oyuncu her yeni araziye geçtiğinde kare donuyordu.
  Alanı 160×160 m'ye (düşmanlar en fazla ~46 m uzakta doğuyor), hücre boyutunu
  0.3 m'ye çekmek yükü ~11 kat azalttı.

- **Düşmanlar, kendilerini örten ekranların arkasından saldırıyordu.** Giriş
  sinematiği ve ömür boyu bir kez gösterilen "nasıl oynanır" paneli kendi
  süreleriyle çalışıyor, spawner ise başlangıç gecikmesini bunlardan bağımsız
  sayıyordu — üstelik oyuncu Start'a bastığı andan itibaren. İkisi hiç
  uyuşmadığı için panel kapandığında ilk dalga çoktan doğmuş ve mesafeyi
  kapatmış oluyordu; oyuncu görmeye başladığı anda hasar yiyordu. Spawn sayacı
  artık kontrolün gerçekten oyuncuya geçmesini *ve* panelin kapanmasını
  bekliyor; bir zaman aşımıyla da düşmanların hiç doğmama ihtimali kapatıldı.

- **Sinematik sırasında ateş edilebiliyor, duraklatılmış oyunda nişan
  alınabiliyordu.** Hareket ile ateş etme iki ayrı bileşende olduğu için,
  sinematikte kontrolcüyü kapatmak ateş etmeyi kapatmıyordu. Duraklatmada ise
  aynı boşluğun daha ince bir hâli vardı: hareket `Time.deltaTime` ile
  çarpıldığından `timeScale = 0`'da kendiliğinden duruyor, ama nişan alma
  `transform.rotation`'ı doğrudan atadığı için karakter duran oyunda imlece
  dönmeye devam ediyordu.

- **Sahne yeniden yüklenince hayalet düşmanlar kalıyordu.** Nesne havuzu
  yeniden başlatmayı atlatabilmek için `DontDestroyOnLoad` işaretli — bu da
  sahne yeniden yüklendiğinde hâlâ hayatta olan düşmanların hiç yok edilmemesi
  demek. Yeni doğuş noktasının hemen yanında, oyuncunun dibinde beliriyorlardı.
  Artık her yeni oyun, önceki turdan kalan tüm `EnemyAI`'ları zorla havuza
  gönderiyor.

- **Tek bir kare ham 3B sahneyi gösteriyordu.** Loading ekranı, Start Menu
  açılmadan önce kendini kapatıyordu; yani bir kare boyunca kamerayı örten
  hiçbir arayüz olmuyor ve oyun dünyası bir an görünüp kayboluyordu. Sırayı
  değiştirmek — önce menüyü aç, sonra loading'i kapat — boşluğu kapatıyor.

- **İyileşme sayısı can doluyken yalan söylüyordu.** İyileşme maksimum cana
  göre sınırlanıyor ama uçan yazı istenen miktarı basıyordu; can doluyken
  alınan bir şifa yine `+30` diyordu. Artık gerçekten iyileşen miktarı, hiç
  iyileşme olmadıysa `FULL HP` yazıyor.

- **Build 60 MB sıkıştırılmamış olarak dağıtılıyordu.** Compression "Disabled"
  ayarlıydı; Gzip'e geçmek — itch.io uygun `Content-Encoding` başlığını
  göndermediği için Decompression Fallback ile birlikte — indirmeyi ~33 MB'a
  düşürdü. WebGL başlangıç belleği de varsayılan 32 MB'dan yükseltildi.

---

## Derleme (WebGL)

- Hedef çözünürlük **1920×1080**
- **Decompression Fallback** açık (sunucu doğru header göndermediğinde
  tarayıcı tarafında açılabilmesi için)
- WebGL içeriği `file://` üzerinden çalışmaz; yerel bir HTTP sunucusu ya da
  gerçek bir barındırma (itch.io) gerekir

---

## Lisans

Kendi kod ve verim **MIT** lisanslı — bkz. [LICENSE](LICENSE).
Üçüncü parti Unity Asset Store içerikleri bu depoya dahil değildir ve kendi
lisanslarına tabidir.
