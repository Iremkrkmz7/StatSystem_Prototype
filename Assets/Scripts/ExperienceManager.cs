using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager instance;

    public static event Action<int> OnLevelUp;

    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    void Awake()
    {
        
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

   
    public void AddXP(int amount)
    {
        currentXP += amount;
        Debug.Log("Kazanılan XP: " + amount + " | Mevcut XP: " + currentXP + "/" + xpToNextLevel);

        
        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentXP -= xpToNextLevel; // Fazladan kazanılan XP boşa gitmesin, yeni levele aktarılsın
        currentLevel++;
        
        
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f); 

        Debug.Log("🎉 SEVİYE ATLANDI! Yeni Seviye: " + currentLevel);
        
        OnLevelUp?.Invoke(currentLevel);
    }
}
