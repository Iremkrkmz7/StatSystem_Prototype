using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Oyuncu level atladiginda oyunu duraklatip 3 rastgele yukseltme secenegi
// sunar (Vampire Survivors tarzi "pick one of three"). Secilen yukseltme
// SCharacterStats'a kalici bir PercentAdd modifier olarak eklenir.
//
// Not: SCharacterStats.HandleLevelUp zaten otomatik can/hasar artisi
// uyguluyor (level basina sabit buyume) - bu sistemi BOZMUYORUZ, sadece
// USTUNE ekstra bir secim katiyoruz. Boylece mevcut denge/testler etkilenmez.
//
// ONEMLI: bu script'in oldugu GameObject (LevelUpPanel) HER ZAMAN aktif kalir
// - GameOverPanel/PausePanel'deki AYNI desen. "panel" alani sadece kartlarin/
// basligin oldugu ic kutuyu (PanelContent) acip kapatir. Eskiden script'in
// KENDI GameObject'ini kapatiyorduk - bu da GameObject sahnede varsayilan
// KAPALI baslarsa Start() hic calismayip abone olunamadigi icin sistem hic
// calismiyordu; ACIK baslarsa da Start() calisana kadar (ayni frame icinde
// olsa bile) panel bir an icin gorunur oluyordu. Simdi kok obje hep acik,
// sadece icerik (PanelContent) ve arka plan karartmasi (dimBackground)
// acilip kapaniyor - hicbir sey script'in calismasina bagli degil.
public class LevelUpUI : MonoBehaviour
{
    [SerializeField] GameObject panel; // PanelContent - kartlarin/basligin oldugu kutu
    [SerializeField] Image dimBackground; // tam ekran karartma - GameObject degil, Image.enabled ile acilir/kapanir
    [SerializeField] SCharacterStats playerStats;
    [SerializeField] UpgradeCardUI[] cards; // 3 kart
    [SerializeField] AudioClip openSound; // ekran ilk acildiginda calinir
    [SerializeField] AudioClip chooseSound; // bir secenek secilince calinir

    [Header("Süre Sınırı")]
    [Tooltip("countdownSound atanmissa OTOMATIK olarak o klibin suresine esitlenir - burada yazan deger sadece countdownSound bos oldugunda kullanilir.")]
    [SerializeField] float chooseTimeLimit = 8f;
    // Slider/Image.Filled KULLANMIYORUZ - Slider'in dolum yonu net degildi,
    // Image.Filled ise sprite atanmadan (m_Sprite bos) hic calismiyor (Unity
    // sprite yoksa Filled turunu tamamen yok sayip her zaman DOLU dikdortgen
    // ciziyor). Bunun yerine Fill dikdortgeninin GENISLIGINI (anchorMax.x)
    // 1'den 0'a dogrudan kucultuyoruz - sprite'a bagli degil, garanti calisir.
    [SerializeField] RectTransform timerFillRect;
    [SerializeField] Image timerFillImage; // dolulukla birlikte renk de gecis yapsin diye (sari -> kirmizi)
    [SerializeField] Color timerFullColor = new Color(0.95f, 0.75f, 0.15f);  // dolu (sari)
    [SerializeField] Color timerEmptyColor = new Color(0.9f, 0.15f, 0.15f);  // bos/kritik (kirmizi)
    [Tooltip("Geri sayim sesi - suresi ne kadarsa geri sayim cubugu da TAM O KADAR surer (birebir senkron).")]
    [SerializeField] AudioClip countdownSound;
    [SerializeField] AudioSource countdownAudioSource; // 2D, kendi uzerimizdeki AudioSource

    [Header("İkonlar (opsiyonel - stat basina bir tane, bos birakilirsa ikonsuz gosterilir)")]
    [SerializeField] Sprite damageIcon;
    [SerializeField] Sprite speedIcon;
    [SerializeField] Sprite healthIcon;
    [SerializeField] Sprite attackSpeedIcon;
    [SerializeField] Sprite critIcon;
    [SerializeField] Sprite defenseIcon;

    static readonly UpgradeOption[] Pool = new UpgradeOption[]
    {
        new UpgradeOption("Power",       "+15% Damage",            StatType.Damage,      0.15f),
        new UpgradeOption("Agility",     "+15% Movement Speed",    StatType.Speed,       0.15f),
        new UpgradeOption("Vitality",    "+20% Max Health",        StatType.MaxHealth,   0.20f),
        new UpgradeOption("Rapid Fire",  "+15% Attack Speed",      StatType.AttackSpeed, 0.15f),
        new UpgradeOption("Luck",        "+8% Critical Chance",    StatType.CritChance,  0.08f),
        new UpgradeOption("Armor",       "+10% Defense",           StatType.Defense,     0.10f),
    };

    // StatType'a gore hangi ikonun kullanilacagini dondurur - Inspector'da
    // tek tek her secenege degil, stat basina TEK BIR ikon atamak yeterli.
    Sprite GetIcon(StatType stat) => stat switch
    {
        StatType.Damage => damageIcon,
        StatType.Speed => speedIcon,
        StatType.MaxHealth => healthIcon,
        StatType.AttackSpeed => attackSpeedIcon,
        StatType.CritChance => critIcon,
        StatType.Defense => defenseIcon,
        _ => null,
    };

    Queue<int> _pendingLevels = new Queue<int>();
    bool _showing;
    UpgradeOption[] _currentOptions;
    Coroutine _timerRoutine;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (dimBackground != null) dimBackground.enabled = false;
        if (playerStats == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerStats = player.GetComponent<SCharacterStats>();
        }
        if (XPSystem.Instance != null)
            XPSystem.Instance.OnLevelUp += OnLevelUp;
    }

    void OnDestroy()
    {
        if (XPSystem.Instance != null)
            XPSystem.Instance.OnLevelUp -= OnLevelUp;
    }

    void OnLevelUp(int newLevel)
    {
        _pendingLevels.Enqueue(newLevel);
        if (!_showing) ShowNext();
    }

    void ShowNext()
    {
        if (_pendingLevels.Count == 0)
        {
            _showing = false;
            return;
        }
        _pendingLevels.Dequeue();
        _showing = true;

        _currentOptions = PickThree();
        for (int i = 0; i < cards.Length && i < _currentOptions.Length; i++)
        {
            int idx = i; // closure icin kopya
            _currentOptions[idx].icon = GetIcon(_currentOptions[idx].stat);
            cards[i].Setup(_currentOptions[idx], () => Choose(idx));
        }

        CameraShake.Instance?.StopImmediate(); // yarim kalmis sarsinti kalintisi ekrani bozmasin
        Time.timeScale = 0f;
        if (dimBackground != null) dimBackground.enabled = true;
        if (panel != null) panel.SetActive(true);

        if (Camera.main != null) Sfx.PlayAt(openSound, Camera.main.transform.position);

        // ESKIDEN chooseTimeLimit'i countdownSound.length'e esitliyorduk ama
        // ses dosyasinin TOPLAM suresi (sessizlik/kuyruk dahil) ile KULAGA
        // GELEN gercek geri sayim suresi ayni degilmis - ses bar bitmeden
        // once susuyordu. Artik chooseTimeLimit'e (Inspector'dan ayarlanan,
        // sesle KULAKLA test edilip elle esitlenecek sabit deger) dokunmuyoruz.
        if (countdownSound != null)
        {
            if (countdownAudioSource != null)
            {
                countdownAudioSource.clip = countdownSound;
                countdownAudioSource.volume = Sfx.Volume;
                countdownAudioSource.Play();
            }
        }

        SetFill(1f);
        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
        _timerRoutine = StartCoroutine(CountdownRoutine());
    }

    // Fill dikdortgeninin sag kenarini (anchorMax.x) 0..1 arasinda ayarlar -
    // 1 = tam dolu, 0 = tamamen bos. Sol kenar (anchorMin.x=0) hep sabit.
    // Ayni anda renk de sari'dan kirmiziya gecis yapar (azaldikca kritiklesir).
    void SetFill(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (timerFillRect != null)
        {
            var a = timerFillRect.anchorMax;
            a.x = t01;
            timerFillRect.anchorMax = a;
        }

        if (timerFillImage != null)
            timerFillImage.color = Color.Lerp(timerEmptyColor, timerFullColor, t01);
    }

    // Suresi icinde secim yapilmazsa hicbir bonus uygulanmadan (hak kaybi)
    // kapatir. Time.timeScale=0 iken calistigi icin UNSCALED zaman kullanir -
    // yoksa geri sayim hic ilerlemezdi (CameraShake'teki ayni mantik).
    IEnumerator CountdownRoutine()
    {
        float t = chooseTimeLimit;
        while (t > 0f)
        {
            SetFill(t / chooseTimeLimit); // 1 (dolu) -> 0 (bos)
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
        SetFill(0f);
        ClosePanel();
        ShowNext(); // sirada baska level varsa hemen ac
    }

    void Choose(int index)
    {
        if (_timerRoutine != null) { StopCoroutine(_timerRoutine); _timerRoutine = null; }

        var opt = _currentOptions[index];
        if (playerStats != null)
            playerStats.AddModifier(opt.stat, new StatModifier(opt.value, opt.modType, order: 1, source: this));

        // Tik sesi artik GlobalButtonSfx tarafindan otomatik calindigi icin
        // burada ayrica calmiyoruz (chooseSound de zaten ayni klipti).

        ClosePanel();
        ShowNext(); // sirada baska level varsa (ayni anda birden fazla level atlandiysa) hemen ac
    }

    void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
        if (dimBackground != null) dimBackground.enabled = false;
        if (countdownAudioSource != null) countdownAudioSource.Stop();
        Time.timeScale = 1f;
    }

    // Tamamen rastgele degil - oyuncunun o anki durumuna gore agirlikli secim.
    // Ornegin can dusukse Dayaniklilik (MaxHealth) secenegi cok daha sik
    // cikma sansi kazanir, boylece "mantikli" hissettiren bir sunum olur.
    UpgradeOption[] PickThree()
    {
        float healthPct = 1f;
        if (playerStats != null && playerStats.MaxHealth > 0f)
            healthPct = playerStats.CurrentHealth / playerStats.MaxHealth;

        var pool = new List<UpgradeOption>(Pool);
        var weights = new List<float>(pool.Count);
        foreach (var opt in pool)
            weights.Add(GetWeight(opt, healthPct));

        int count = Mathf.Min(3, pool.Count);
        var result = new UpgradeOption[count];
        for (int n = 0; n < count; n++)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += weights[i];

            float r = Random.value * total;
            float cum = 0f;
            int pick = weights.Count - 1;
            for (int i = 0; i < weights.Count; i++)
            {
                cum += weights[i];
                if (r <= cum) { pick = i; break; }
            }

            result[n] = pool[pick];
            pool.RemoveAt(pick);
            weights.RemoveAt(pick);
        }
        return result;
    }

    // Taban agirlik 1 - can durumuna gore ilgili secenek agirlikca cok artar,
    // ama diger secenekler yine de cikabilir (tamamen elenmiyor).
    float GetWeight(UpgradeOption opt, float healthPct)
    {
        float w = 1f;
        if (opt.stat == StatType.MaxHealth)
        {
            if (healthPct < 0.3f) w *= 6f;      // can cok kritikse neredeyse garanti cikacak kadar agirlikli
            else if (healthPct < 0.6f) w *= 3f; // can orta-dusukse belirgin sekilde daha sik
        }
        return w;
    }
}
