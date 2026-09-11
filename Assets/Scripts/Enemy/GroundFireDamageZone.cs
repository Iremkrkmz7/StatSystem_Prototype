using UnityEngine;

// Bomber olunce yerde kalan alev halkasina eklenir - icinde duran oyuncuya
// (varsayilan olarak sadece oyuncuya) periyodik hasar verir. Obje yok
// edildiginde (EnemyAI'daki Destroy(groundFx, sure)) otomatik sona erer.
public class GroundFireDamageZone : MonoBehaviour
{
    [SerializeField] float radius = 3.5f;
    [SerializeField] float damagePerTick = 8f;
    [SerializeField] float tickInterval = 0.5f;
    [Tooltip("Oyuncu bu alevden hasar aldigi HER tikte calinacak ses")]
    [SerializeField] AudioClip playerHitSound;

    float _timer;
    AudioSource _audioSource; // 2D - PlayClipAtPoint (3D) heal sesindeki gibi kamera uzak/yuksekteyse duyulmuyordu

    void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D - mesafeden bagimsiz hep ayni seviyede duyulsun
    }

    public void Configure(float newRadius, float newDamagePerTick, float newTickInterval, AudioClip newPlayerHitSound = null)
    {
        radius = newRadius;
        damagePerTick = newDamagePerTick;
        tickInterval = newTickInterval;
        if (newPlayerHitSound != null) playerHitSound = newPlayerHitSound;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = tickInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var col in hits)
        {
            if ((col.CompareTag("Player") || col.CompareTag("Enemy")) && col.TryGetComponent<SCharacterStats>(out var stats))
            {
                stats.TakeDamage(damagePerTick);
                // Sadece oyuncu icin - arka plandaki ambiyans/muzigi KESMEDEN,
                // kendi 2D AudioSource'umuzdan PlayOneShot ile calar.
                if (col.CompareTag("Player"))
                    Sfx.PlayOneShot(_audioSource, playerHitSound);
            }
        }
    }
}
