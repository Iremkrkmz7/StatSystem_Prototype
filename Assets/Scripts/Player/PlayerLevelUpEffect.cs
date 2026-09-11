using UnityEngine;

// XPSystem.OnLevelUp - gercek oyunda kullanilan aktif XP/level sistemi
// (EnemyAI, XPSystem.Instance.AddXP() cagiriyor; UIManager de bunu dinliyor).
// SCharacterStats.HandleLevelUp farkli/kullanilmayan bir sisteme (ExperienceManager)
// bagli oldugu icin ona eklemedik - burada dogrudan XPSystem'i dinliyoruz.
public class PlayerLevelUpEffect : MonoBehaviour
{
    [SerializeField] GameObject levelUpVFXPrefab;
    [SerializeField] float vfxDuration = 2f;

    void OnEnable()
    {
        if (XPSystem.Instance != null)
            XPSystem.Instance.OnLevelUp += OnLevelUp;
    }
    void OnDisable()
    {
        if (XPSystem.Instance != null)
            XPSystem.Instance.OnLevelUp -= OnLevelUp;
    }

    void OnLevelUp(int newLevel)
    {
        if (levelUpVFXPrefab == null) return;
        var fx = Instantiate(levelUpVFXPrefab, transform.position, Quaternion.identity, transform);
        Destroy(fx, vfxDuration);
    }
}
