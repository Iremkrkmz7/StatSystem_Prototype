using UnityEngine;

// HitEffect.cs ile ayni pattern: enable olunca kendi suresi kadar oynar,
// sonra havuza geri doner. Explosion.prefab looping oldugu icin suresiz
// oynamaya devam etmesin diye burada elle kesiyoruz.
//
// Follow(): olum animasyonu sirasinda (root motion ile) model hareket
// edebildigi icin, patlama spawn anindaki sabit pozisyonda kalirsa "geride
// kalmis" gibi gorunuyordu. Artik her frame hedefin GUNCEL pozisyonuna
// gore (spawn anindaki gorece offset korunarak) kendini yeniden konumluyor.
public class DeathExplosion : MonoBehaviour
{
    [SerializeField] float destroyTime = 0.6f;

    Transform _followTarget;
    Vector3 _offsetFromTarget;
    float _fixedY;

    public void Follow(Transform target)
    {
        _followTarget = target;
        _offsetFromTarget = target != null ? transform.position - target.position : Vector3.zero;
        _fixedY = transform.position.y;
    }

    void OnEnable()
    {
        Invoke(nameof(ReturnToPool), destroyTime);
    }

    void Update()
    {
        if (_followTarget != null)
        {
            // Yatayda (X/Z) hedefi YUMUSAK takip et (root motion kaymasi icin) -
            // aninda yapisirsa govdeyle birlikte one suruklenip "one kaymis" gibi
            // duruyordu, hic takip etmezse de govde ilerleyince "geride kalmis"
            // gibi duruyordu. Gecikmeli takip ikisi arasinda denge kuruyor.
            Vector3 targetPos = _followTarget.position + _offsetFromTarget;
            targetPos.y = _fixedY;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 4f);
        }
    }

    void ReturnToPool()
    {
        _followTarget = null;
        PoolManager.Instance?.Return("HeadBurst", gameObject);
    }
}
