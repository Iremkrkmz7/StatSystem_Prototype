using UnityEngine;

// Hasar/heal/XP gibi her turlu ucan yazi icin tek merkezi giris noktasi.
// SCharacterStats hem player'da hem enemy'lerde oldugu icin (EnemyDataSO gibi
// enemy'ye ozel bir veri kaynagi yerine) buradan cagirmak, prefab referansini
// her karakterde tekrar tekrar atamaktan daha temiz.
public class FloatingTextSpawner : MonoBehaviour
{
    public static FloatingTextSpawner Instance { get; private set; }

    [SerializeField] GameObject floatingTextPrefab;

    void Awake() => Instance = this;

    public void Show(Vector3 worldPos, string text, Color color, Transform follow = null)
    {
        if (floatingTextPrefab == null) return;
        var go = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity);
        go.GetComponent<FloatingText>()?.SetText(text, color, follow);
    }
}
