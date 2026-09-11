using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] Camera mainCamera;

    SCharacterStats _stats;

    void Start()
    {
        _stats = GetComponentInParent<SCharacterStats>();
        if(_stats == null)
         {
            _stats = transform.parent?.parent?.GetComponent<SCharacterStats>();
         }
         if(_stats == null)
         {
            Debug.LogError("SCharacterStats bulunamadı!1!!");
            return;
         }

        _stats.OnHealthChanged += UpdateBar;
        UpdateBar(_stats.CurrentHealth, _stats.MaxHealth);
        
        if(mainCamera == null)
        mainCamera = Camera.main;
    }
    void UpdateBar(float current, float max)
    {
        if(slider== null) return;
        slider.value = max > 0 ? current / max : 0;
    }
    void LateUpdate()
    {
        if(mainCamera == null) return;
        // Kameranin tum rotasyonunu (pitch dahil) kopyalamak yerine sadece
        // yatay (yaw) donusunu takip ediyoruz, dik duruyoruz. Boylece
        // canvas'in "yukarisi" her zaman gercek dunya yukarisi olur; kamera
        // egik baktigi icin tum rotasyonu kopyalamak, uzerindeki her offset'i
        // (slider'in konumu dahil) kismen ileri/geri kaydiriyordu.
        Vector3 dir = transform.position - mainCamera.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
    void OnDestroy()
    {
        if(_stats != null)
        _stats.OnHealthChanged -= UpdateBar;
    }

}
