using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance { get; private set; }

    // Degerler artik BalanceConfig'ten (Assets/Resources/BalanceConfig.asset)
    // geliyor - GameBalance.xlsx -> CSV -> Tools>Balance>Import ile guncellenir.
    static BalanceConfig Cfg => BalanceConfig.Instance;

    int _currentXP = 0;
    int _currentLevel = 1;

    public int CurrentXP    => _currentXP;
    public int CurrentLevel => _currentLevel;
    public int XPRequired   => Mathf.RoundToInt(Cfg.baseXPRequired * Mathf.Pow(Cfg.xpMultiplier, _currentLevel - 1));

    public event Action<int, int> OnXPChanged;   // (current, required)
    public event Action<int>      OnLevelUp;      // (newLevel)

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddXP(int amount)
    {
        _currentXP += amount;
        OnXPChanged?.Invoke(_currentXP, XPRequired);

        while (_currentXP >= XPRequired)
        {
            _currentXP -= XPRequired;
            _currentLevel++;
            OnLevelUp?.Invoke(_currentLevel);
            Debug.Log("Level Up! Seviye: " + _currentLevel);
        }
    }

    public float GetXPPercent() => (float)_currentXP / XPRequired;

    // "Kaldigin yerden devam" - run basinda (SaveSystem.BestWave'e gore)
    // seviyeyi dogrudan ayarlamak icin. NORMAL AddXP akisindan (OnLevelUp
    // ATESLEMEZ) FARKLI - yoksa LevelUpUI'nin "upgrade sec" ekrani N kere
    // ust uste acilirdi. Stat buyumesi icin SCharacterStats.ApplyCatchUpLevels
    // AYRICA cagrilmali (XPSystem stat bilmiyor, bu ayri bir sorumluluk).
    public void SetLevelSilently(int level)
    {
        _currentLevel = Mathf.Max(1, level);
        _currentXP = 0;
        OnXPChanged?.Invoke(_currentXP, XPRequired);
    }
}

