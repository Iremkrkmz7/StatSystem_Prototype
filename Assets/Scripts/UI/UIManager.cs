using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Level ve XP Arayüzü")]
    public Image xpBarFill; 
    public TextMeshProUGUI xpText; 
    public TextMeshProUGUI levelText; 

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // XPSystem çalışıyorsa, XP veya Level değiştiğinde UI'ı haberdar et diyoruz.
        if (XPSystem.Instance != null)
        {
            XPSystem.Instance.OnXPChanged += UpdateXPBar;
            XPSystem.Instance.OnLevelUp += UpdateLevel;

            // Oyun ilk başladığında barı ve yazıyı başlangıç değerleriyle doldur
            UpdateLevel(XPSystem.Instance.CurrentLevel);
            UpdateXPBar(XPSystem.Instance.CurrentXP, XPSystem.Instance.XPRequired);
        }
    }

    void OnDestroy()
    {
        // Sahne değişirken veya obje silinirken hata almamak için abonelikleri iptal ediyoruz
        if (XPSystem.Instance != null)
        {
            XPSystem.Instance.OnXPChanged -= UpdateXPBar;
            XPSystem.Instance.OnLevelUp -= UpdateLevel;
        }
    }

    public void UpdateXPBar(int currentXP, int targetXP)
    {
        // Barın doluluk oranını yumuşakça doldurmak için float kullanıyoruz
        xpBarFill.fillAmount = (float)currentXP / targetXP;
        xpText.text = $"XP : {currentXP} / {targetXP}";

        // Level yazisini da senkron tut - "kaldigin yerden devam" (SetLevelSilently)
        // OnLevelUp ATESLEMEDEN seviyeyi degistiriyor, o yuzden HUD level'i
        // eski (Level 1) takili kalabiliyordu. OnXPChanged her durumda
        // atesleniyor, buradan tazeliyoruz.
        if (levelText != null && XPSystem.Instance != null)
            levelText.text = XPSystem.Instance.CurrentLevel.ToString();
    }

    public void UpdateLevel(int newLevel)
    {
        levelText.text = newLevel.ToString();
        // Level atlayınca bar görsel olarak da sıfırlansın
        xpBarFill.fillAmount = 0f; 
    }
}
