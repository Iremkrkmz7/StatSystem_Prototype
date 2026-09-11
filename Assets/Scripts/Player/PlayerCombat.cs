using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent(typeof(SCharacterStats))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] WeaponHandler weaponHandler;

    SCharacterStats _stats;
    Animator        _animator;

    // Raycast icin her frame yeni nesne olusturmamak adina tek seferlik cache'ler
    // (surekli LMB basiliyken her frame 'new PointerEventData' allocate etmek
    // GC copu biriktirip uzun oyun oturumlarinda kasma/takilmaya sebep oluyordu).
    static PointerEventData _pointerEventData;
    static List<RaycastResult> _raycastResults = new List<RaycastResult>();

    bool _mouseDownOnButton; // bu basili tutma boyunca butonun ustunden mi baslamis

   void Awake()
    {
        _stats    = GetComponent<SCharacterStats>();
        _animator = GetComponentInChildren<Animator>();
    }
    void Update()
    {
        if (_stats.IsDead) return;
        if (Time.timeScale <= 0f) return; // oyun duraklatılmışken (menu/pause) ateş etme

        // Sadece tiklamanin BASLADIGI frame'de (her frame degil) UI kontrolu yap -
        // hem performansli hem de gercek bir butona (START, Pause vs.) tiklama/basili
        // tutma ates etmeyi engellemeye yetiyor.
        if (Input.GetMouseButtonDown(0))
            _mouseDownOnButton = IsPointerOverButton();

        if (!Input.GetMouseButton(0) || _mouseDownOnButton) return;

        weaponHandler?.TryShoot();
        if (Input.GetMouseButtonDown(0))
    {
    _animator?.SetInteger("ShootType", Random.Range(0, 3));
    _animator?.SetTrigger("Shoot");
    }
    }

    // Sadece gercekten tiklanabilir (Button vs.) bir UI elemaninin ustundeysek true doner.
    // Sahnede aktif kalmis, gorunmez tam-ekran raycast-target'li panellerin (video arka
    // plani gibi) ates etmeyi tumden engellemesini onlemek icin genel
    // EventSystem.IsPointerOverGameObject() yerine bunu kullaniyoruz.
    bool IsPointerOverButton()
    {
        if (EventSystem.current == null) return false;

        if (_pointerEventData == null) _pointerEventData = new PointerEventData(EventSystem.current);
        _pointerEventData.position = Input.mousePosition;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        foreach (var result in _raycastResults)
            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;

        return false;
    }

    public void EquipWeapon(WeaponSO weapon) =>
        weaponHandler?.EquipWeapon(weapon);
}