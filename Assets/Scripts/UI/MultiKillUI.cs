using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// GameEvents.OnEnemyKilled'i dinler, kisa bir sure icinde (comboWindow) art
// arda kac dusman oldurulduyse ona gore "DOUBLE KILL!" / "TRIPLE KILL!" /
// "QUADRA KILL!" / "PENTA KILL!" banner'ini gosterir (LoL/FPS oyunlarindaki gibi).
// Her seviye icin bir gorsel (Sprite) atanmissa arka plan cercevesi olarak
// gosterilir, "DOUBLE KILL!" vs. yazisi (bannerText) her zaman ustunde durur.
public class MultiKillUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI bannerText;
    [SerializeField] Image bannerImage;
    [SerializeField] CanvasGroup bannerGroup;
    [SerializeField] float comboWindow = 3f;
    [SerializeField] float showDuration = 1.2f;
    [SerializeField] float fadeDuration = 0.25f;
    [Tooltip("Banner (DOUBLE KILL! vs.) ekrana her ciktiginda calinacak ses")]
    [SerializeField] AudioClip killStreakSound;
    [SerializeField] AudioSource audioSource; // 2D UI sesi icin - kendi uzerimizdeki AudioSource

    [Header("Gorseller (opsiyonel - bos birakilirsa renkli yazi kullanilir)")]
    [SerializeField] Sprite doubleSprite;
    [SerializeField] Sprite tripleSprite;
    [SerializeField] Sprite quadraSprite;
    [SerializeField] Sprite pentaSprite;

    [Header("Renkler (gorsel atanmadiginda yazi icin kullanilir)")]
    [SerializeField] Color doubleColor = new Color(0.25f, 0.65f, 1f);
    [SerializeField] Color tripleColor = new Color(0.95f, 0.75f, 0.15f);
    [SerializeField] Color quadraColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] Color pentaColor = new Color(0.75f, 0.25f, 0.95f);

    int _streak;
    float _lastKillTime = -100f;
    Coroutine _showRoutine;

    void Start()
    {
        if (bannerGroup != null) bannerGroup.alpha = 0f;
        GameEvents.OnEnemyKilled.AddListener(OnEnemyKilled);
    }

    void OnDestroy()
    {
        GameEvents.OnEnemyKilled.RemoveListener(OnEnemyKilled);
    }

    void OnEnemyKilled()
    {
        if (Time.time - _lastKillTime <= comboWindow)
            _streak++;
        else
            _streak = 1;
        _lastKillTime = Time.time;

        if (_streak < 2) return;

        string label;
        Color color;
        Sprite sprite;
        int bonusXP;
        switch (_streak)
        {
            case 2: label = "DOUBLE KILL!"; color = doubleColor; sprite = doubleSprite; bonusXP = 10; break;
            case 3: label = "TRIPLE KILL!"; color = tripleColor; sprite = tripleSprite; bonusXP = 20; break;
            case 4: label = "QUADRA KILL!"; color = quadraColor; sprite = quadraSprite; bonusXP = 35; break;
            default: label = "PENTA KILL!"; color = pentaColor; sprite = pentaSprite; bonusXP = 50; break; // 5+
        }

        XPSystem.Instance?.AddXP(bonusXP);
        QuestManager.Instance?.OnMultiKill(_streak);

        Sfx.PlayOneShot(audioSource, killStreakSound);

        // Gorsel atanmissa arka plan cercevesi olarak gosterilir, yazi (DOUBLE KILL! vs.)
        // her zaman ustunde kalir.
        if (bannerImage != null)
        {
            bannerImage.sprite = sprite;
            bannerImage.enabled = sprite != null;
        }
        if (bannerText != null)
        {
            bannerText.text = label;
            bannerText.color = color;
            bannerText.enabled = true;
        }

        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(ShowBanner());
    }

    IEnumerator ShowBanner()
    {
        if (bannerGroup == null) yield break;

        // Hepsi unscaled - Time.timeScale=0 oldugunda (orn. ayni anda bir
        // Level-Up ekrani acildiginda) t hic ilerlemeyip banner yarim solmus
        // halde sonsuza kadar ekranda asili kalmasin diye (CameraShake/
        // EnemyShake'teki ayni bug).
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            bannerGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        bannerGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(showDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            bannerGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        bannerGroup.alpha = 0f;
    }
}
