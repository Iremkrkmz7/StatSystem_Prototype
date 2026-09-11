 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance {get; private set;}
    [SerializeField] List<AchievementSO> achievements = new();
    readonly Dictionary<string, int> _stats = new();
    public event Action<AchievementSO> OnAchievementUnlocked;
    void Awake()
    {
        if(Instance!= null) {Destroy(gameObject); return; }
        Instance = this;

        // ONEMLI: acilan basarimlar ONCEDEN hicbir yerde kaydedilmiyordu -
        // her yeni oturumda TUM basarimlar sifirlanip tekrar tekrar
        // acilabiliyordu. Burada daha once acilmis olanlari SESSIZCE
        // (OnAchievementUnlocked ATESLEMEDEN - "basarim acildi!" bildirimi
        // her seferinde tekrar cikmasin diye) geri yukluyoruz.
        foreach (var a in achievements)
        {
            if (a != null && SaveSystem.IsAchievementUnlocked(a.AchievementName))
                a.IsUnlocked = true;
        }
    }
    public void Increment(string key, int amount= 1)
    {
        _stats.TryGetValue(key, out int current);
        _stats[key] = current + amount;
        CheckAll(key);
    }
    public int GetStat(string key) => 
    _stats.TryGetValue(key, out int v) ? v : 0;

    void CheckAll(string key)
    {
        foreach(var a in achievements)
        {
            if(a.IsUnlocked || a.TrackedKey != key) continue;
            if(_stats[key] < a.RequiredCount) continue;

            a.IsUnlocked = true;
            SaveSystem.UnlockAchievement(a.AchievementName);
            OnAchievementUnlocked?.Invoke(a);
            Debug.Log($"Achievement açıldı: {a.AchievementName}");
        }
    }

    
}
