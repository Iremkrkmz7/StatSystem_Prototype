using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementNotificationUI : MonoBehaviour
{
    [SerializeField] CanvasGroup      canvasGroup;
    [SerializeField] TextMeshProUGUI  titleText;
    [SerializeField] TextMeshProUGUI  descText;
    [Tooltip("Sadece level up aninda kullanilan, ayri text objesi (achievement title/desc'ten bagimsiz).")]
    [SerializeField] TextMeshProUGUI  levelUpText;
    [Tooltip("Level atlayinca OnLeveledUp event'ini dinlemek icin - Player'in kendisini surukle.")]
    [SerializeField] SCharacterStats  playerStats;
    [Tooltip("Sadece level up aninda gorunen ikonlar (kalp/yildirim) - achievement'ta gizlenir.")]
    [SerializeField] GameObject       hpIcon;
    [SerializeField] GameObject       damageIcon;
    [SerializeField] float            displayDuration = 3f;
    [SerializeField] float            fadeDuration    = 0.4f;

    void Start()
    {
        if(canvasGroup) canvasGroup.alpha = 0f;
        if(AchievementManager.Instance != null)
        AchievementManager.Instance.OnAchievementUnlocked += Show;
        // XPSystem.OnLevelUp yerine playerStats.OnLeveledUp'i dinliyoruz -
        // gosterecegimiz +HP/+Damage miktarlarini SCharacterStats zaten
        // GERCEKTEN hesaplayip uyguluyor, biz formulu burada tekrar
        // yazip koddan sapma riski almiyoruz.
        if (playerStats != null)
            playerStats.OnLeveledUp += ShowLevelUp;
    }
    void Show(AchievementSO achievement)
    {
        StopAllCoroutines();
        if (levelUpText) levelUpText.text = "";
        if (hpIcon) hpIcon.SetActive(false);
        if (damageIcon) damageIcon.SetActive(false);
        StartCoroutine(ShowRoutine(achievement.AchievementName, achievement.Description));
    }
    void ShowLevelUp(int newLevel, float healthGain, float damageGain)
    {
        StopAllCoroutines();
        if (levelUpText) levelUpText.text = "LEVEL UP!";
        if (titleText) titleText.text = $"LEVEL {newLevel}";
        if (descText) descText.text = $"Congrats!\nYou've reached this level!\n\n+{healthGain:0.#} MAX HP\n+{damageGain:0.#} DAMAGE";        if (hpIcon) hpIcon.SetActive(true);
        if (damageIcon) damageIcon.SetActive(true);

        StartCoroutine(FadeInOutRoutine());
    }
    IEnumerator ShowRoutine(string title, string desc)
    {
        if(titleText) titleText.text = title;
        if(descText) descText.text = desc;
        yield return FadeInOutRoutine();
    }
    IEnumerator FadeInOutRoutine()
    {
        yield return FadeTo(1f,fadeDuration);
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return FadeTo(0f, fadeDuration);
    }
    IEnumerator FadeTo(float target, float duration)
    {
        if(canvasGroup == null) yield break;
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            // Unscaled - Time.timeScale=0 iken (Level-Up/Pause ekrani) bildirim
            // yarim solmus halde takili kalmasin.
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = target;
    }
    void OnDestroy()
    {
        if (AchievementManager.Instance != null)
          AchievementManager.Instance.OnAchievementUnlocked -= Show;
        if (playerStats != null)
            playerStats.OnLeveledUp -= ShowLevelUp;
    }
}
