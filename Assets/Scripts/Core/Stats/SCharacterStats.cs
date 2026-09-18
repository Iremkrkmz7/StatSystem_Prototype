using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[DisallowMultipleComponent]
public class SCharacterStats : MonoBehaviour
{
  [Header("Base Statlar")]
    [SerializeField] float baseMaxHealth   = 100f;
    [SerializeField] float baseSpeed       = 5f;
    [SerializeField] float baseDamage      = 10f;
    [SerializeField] float baseDefense     = 0f;
    [SerializeField] float baseAttackSpeed = 1f;
    [SerializeField] float baseCritChance  = 0.05f;

  [Header("Ses")]
    [SerializeField] AudioClip levelUpSound;
    [SerializeField] AudioClip healSound;
    [Range(0f, 2f)] [SerializeField] float healSoundVolume = 1.5f;

    // Player level-up can/hasar artislari artik BalanceConfig'ten
    // (GameBalance.xlsx -> CSV -> Tools>Balance>Import). Sadece Player'da
    // kullanilir (HandleLevelUp CompareTag("Player") ile guardli).
    float healthIncreasePerLevel => BalanceConfig.Instance.healthIncreasePerLevel;
    float damageIncreasePerLevel => BalanceConfig.Instance.damageIncreasePerLevel;

    public float HealthIncreasePerLevel => healthIncreasePerLevel;
    public float DamageIncreasePerLevel => damageIncreasePerLevel;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;
    // newLevel, o levelde GERCEKTEN uygulanan can/hasar bonusu (level'e gore
    // buyuyen, sabit degil) - UI bu degerleri kendi hesaplamasin diye buradan veriyoruz.
    public event Action<int, float, float> OnLeveledUp;


    Dictionary<StatType, CharacterStat> _stats;
    float _currentHealth;
    bool  _isDead;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => GetStat(StatType.MaxHealth);
    public bool  IsDead => _isDead;

    void Awake()
    {
        _stats = new Dictionary<StatType , CharacterStat>
        {
             { StatType.MaxHealth,   new CharacterStat(baseMaxHealth)   },
            { StatType.Speed,       new CharacterStat(baseSpeed)       },
            { StatType.Damage,      new CharacterStat(baseDamage)      },
            { StatType.Defense,     new CharacterStat(baseDefense)     },
            { StatType.AttackSpeed, new CharacterStat(baseAttackSpeed) },
            { StatType.CritChance,  new CharacterStat(baseCritChance)  },
        };
        _currentHealth = GetStat(StatType.MaxHealth);
    }
    public float GetStat(StatType type) =>
    _stats.TryGetValue(type, out var s) ? s.Value : 0f;

    public void AddModifier(StatType type, StatModifier modifier)
    {
        if(_stats.TryGetValue(type, out var stat))
        stat.AddModifier(modifier);
    }
    public void RemoveModifiersFromSource(object source)
    {
        foreach(var stat in _stats.Values)
        stat.RemoveAllFromSource(source);

    }
    public void ApplyBuff(BuffSO buff)
    {
        if(buff != null)
        StartCoroutine(BuffRoutine(buff));
    }
    IEnumerator BuffRoutine(BuffSO buff)
    {
        foreach (var entry in buff.Modifiers)
        AddModifier(entry.StatType, new StatModifier(entry.Value, entry.ModType, source: buff));

        if(buff.Duration > 0)
        {
            yield return new WaitForSeconds(buff.Duration);
            RemoveModifiersFromSource(buff);
        }
    }
    public void TakeDamage(float rawDamage)
    {
        // Olu bir karaktere (orn. yerdeki alevden, gec kalan bir vurustan)
        // tekrar tekrar hasar denemesi gelirse kamera sarsintisi da tekrar
        // tekrar tetiklenip surekli titreme hissi veriyordu - artik en once
        // olum kontrolu yapiyoruz.
        if(_isDead) return;

        if (CompareTag("Player"))
        CameraShake.Instance?.Shake();
    else
        GetComponent<EnemyShake>()?.Shake();

        float defense = GetStat(StatType.Defense);
        float actualDamage = Mathf.Max(1f ,rawDamage - defense);
        _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);

        FloatingTextSpawner.Instance?.Show(transform.position + Vector3.up * 2f, actualDamage.ToString("0"), Color.red, transform);

        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        if(_currentHealth <= 0f) Die();
    }
    // showFloatingText: level atlarken cani tamamen doldurmak icin de bu
    // metot kullaniliyor (HandleLevelUp) - o zaten kendi buyuk panelini
    // gosterdigi icin orada ayrica "+X HP" ucusmasin diye false gecilir.
    public void Heal(float amount, bool showFloatingText = true)
    {
        if(_isDead) return;
        float before = _currentHealth;
        _currentHealth = Mathf.Min(GetStat(StatType.MaxHealth), _currentHealth + amount);
        // GERCEKTEN iyilesen miktar - can zaten doluyken (ya da tavana cok yakinken)
        // istenen miktarin tamami uygulanmiyor. Eskiden yazi her durumda "+30"
        // diyordu, hic iyilesme olmasa bile - yaniltiyordu.
        float healed = _currentHealth - before;
        if (showFloatingText)
        {
            bool didHeal = healed > 0.01f;
            FloatingTextSpawner.Instance?.Show(
                transform.position + Vector3.up * 2f,
                didHeal ? $"+{healed:0}" : "FULL HP",
                didHeal ? Color.green : Color.white,
                transform);
            // showFloatingText false ise (level atlarken tam can doldurma gibi) bu
            // zaten kendi sesine (levelUpSound) sahip, ustune binmesin diye burada calmiyoruz.
            Sfx.PlayAt(healSound, transform.position, healSoundVolume);
        }
        OnHealthChanged?.Invoke(_currentHealth , MaxHealth);
    }
  public void Revive (float healthPercent = 1f)
  {
    _isDead = false;
    _currentHealth = MaxHealth * Mathf.Clamp01(healthPercent);
    OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
  }
  void Die()
  {
    _isDead = true;
    OnDied?.Invoke();
  }
  private void OnEnable()
    {
        // ExperienceManager hic tetiklenmeyen (AddXP'i hicbir yerden cagrilmayan)
        // olu bir sistemdi, HandleLevelUp gercekte hic calismiyordu. Gercek oyunda
        // kullanilan aktif sistem XPSystem (EnemyAI XP'yi oraya veriyor). Ayrica
        // bu component hem player'da hem enemy'lerde var - guard olmadan enemy'ler
        // de player level atlayinca guclenirdi, o yuzden sadece Player'a uyguluyoruz.
        if (CompareTag("Player") && XPSystem.Instance != null)
            XPSystem.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (CompareTag("Player") && XPSystem.Instance != null)
            XPSystem.Instance.OnLevelUp -= HandleLevelUp;
    }

    // "Kaldigin yerden devam" (SaveSystem.BestWave'e gore) - run basinda
    // seviye dogrudan N'e sicratilirken, normalde 1'den N'e kadar HER
    // seviyede TEK TEK eklenecek olan health/damage artislarinin TOPLAMINI
    // burada TEK seferde, sessizce (ses/LevelUpUI upgrade-secme ekrani
    // ACMADAN - o ekran N kere ust uste acilirsa oyuncu daha oyun
    // baslamadan N tane secim yapmak zorunda kalirdi) uyguluyoruz.
    // HandleLevelUp'taki "rate * newLevel" artisinin 2..level toplami:
    // sum(2..level) = level*(level+1)/2 - 1.
    public void ApplyCatchUpLevels(int level)
    {
        if (level <= 1) return;
        float sumK = level * (level + 1) / 2f - 1f;
        float totalHealthGain = healthIncreasePerLevel * sumK;
        float totalDamageGain = damageIncreasePerLevel * sumK;

        AddModifier(StatType.MaxHealth, new StatModifier(totalHealthGain, StatModifier.ModifierType.Flat, source: this));
        AddModifier(StatType.Damage, new StatModifier(totalDamageGain, StatModifier.ModifierType.Flat, source: this));
        Heal(MaxHealth, showFloatingText: false);
    }

    private void HandleLevelUp(int newLevel)
    {
        // Bonus artik sabit degil, level'le birlikte buyuyor (level 1'de 1x,
        // level 10'da 10x taban artis) - "olur" onayiyla eklendi.
        float healthGain = healthIncreasePerLevel * newLevel;
        float damageGain = damageIncreasePerLevel * newLevel;

        AddModifier(StatType.MaxHealth, new StatModifier(healthGain, StatModifier.ModifierType.Flat, source: this));
        AddModifier(StatType.Damage, new StatModifier(damageGain, StatModifier.ModifierType.Flat, source: this));
        // B. CANI TAMAMEN DOLDUR (Opsiyonel ama çok iyi hissettirir)
        // Senin kodundaki Heal fonksiyonunu kullanıyoruz, MaxHealth'i kendi bulup ona göre sınırlandırıyor.
        Heal(MaxHealth, showFloatingText: false);

        Sfx.PlayAt(levelUpSound, transform.position);

        Debug.Log($"Seviye {newLevel} - Karakter Güçlendi! Yeni Max Can: {MaxHealth} | Yeni Hasar: {GetStat(StatType.Damage)}");

        OnLeveledUp?.Invoke(newLevel, healthGain, damageGain);
    }
}