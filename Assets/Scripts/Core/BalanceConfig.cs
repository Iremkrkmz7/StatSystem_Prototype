using UnityEngine;

// DATA-DRIVEN DENGE AYARI - butun GLOBAL tunable degerler burada, tek yerde.
// XPSystem / SCharacterStats / EnemySpawner / AbilityController artik kendi
// SerializeField'larindan DEGIL bu asset'ten okuyor.
//
// Duzenleme: ya Inspector'dan, ya da GameBalance.xlsx'i CSV export edip
// Tools > Balance > Import from CSV ile. (Enemy tipi statlari EnemyDataSO'da,
// onlar da ayni menuden bir enemy CSV'siyle guncelleniyor.)
//
// Asset KONUMU: Assets/Resources/BalanceConfig.asset  (Resources.Load ile
// runtime'da sahne referansi olmadan yuklenir).
[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Balance/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    // ---------- singleton erisim ----------
    static BalanceConfig _instance;
    public static BalanceConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<BalanceConfig>("BalanceConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("[BalanceConfig] Assets/Resources/BalanceConfig.asset bulunamadi - runtime'da olusturulan varsayilanlar kullanilacak.");
                    _instance = CreateInstance<BalanceConfig>();
                }
            }
            return _instance;
        }
    }

    [Header("XP & LEVEL (XPSystem)")]
    public int   baseXPRequired = 140;
    public float xpMultiplier   = 1.3f;

    [Header("PLAYER LEVEL-UP GAINS (SCharacterStats, sadece Player)")]
    [Tooltip("Her level-up'ta: +healthIncreasePerLevel * yeniLevel HP")]
    public float healthIncreasePerLevel = 8f;
    [Tooltip("Her level-up'ta: +damageIncreasePerLevel * yeniLevel hasar")]
    public float damageIncreasePerLevel = 2.5f;

    [Header("WAVE SPAWNING (EnemySpawner)")]
    public int   baseEnemyCount    = 3;
    public int   countIncrement    = 1;
    public int   maxWaveEnemyCount  = 15;
    public float spawnRadius       = 28f;
    public float spawnInterval     = 0.4f;
    public float waveCooldown      = 8f;
    public float startupDelay      = 5f;

    [Header("ABILITY (AbilityController)")]
    [Tooltip("effectiveCooldown = Cooldown / (1 + abilityCDLevelFactor * playerLevel)")]
    public float abilityCDLevelFactor = 0.05f;

    // CSV importer'in cagirdigi - bir 'key' string'ine gore alani gunceller.
    // Bilinmeyen key sessizce atlanir (loglanir). Bool doner: tanindi mi.
    public bool ApplyKey(string key, float value)
    {
        switch (key.Trim())
        {
            case "baseXPRequired":          baseXPRequired = Mathf.RoundToInt(value); return true;
            case "xpMultiplier":            xpMultiplier = value; return true;
            case "healthIncreasePerLevel":  healthIncreasePerLevel = value; return true;
            case "damageIncreasePerLevel":  damageIncreasePerLevel = value; return true;
            case "baseEnemyCount":          baseEnemyCount = Mathf.RoundToInt(value); return true;
            case "countIncrement":          countIncrement = Mathf.RoundToInt(value); return true;
            case "maxWaveEnemyCount":       maxWaveEnemyCount = Mathf.RoundToInt(value); return true;
            case "spawnRadius":             spawnRadius = value; return true;
            case "spawnInterval":           spawnInterval = value; return true;
            case "waveCooldown":            waveCooldown = value; return true;
            case "startupDelay":            startupDelay = value; return true;
            case "abilityCDLevelFactor":    abilityCDLevelFactor = value; return true;
            default: return false;
        }
    }
}
