using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sahnedeki (ve LevelUp kartlari gibi sonradan olusan) HER Button/Selectable
// tiklandiginda otomatik olarak ayni tik sesini calar. Tek tek her butonun
// OnClick'ine ayri ayri ses eklemek yerine (bazilari unutulup sessiz kaliyordu,
// bazilarinda da iki script ayni sesi ayni anda calip ustuste biniyordu)
// merkezi olarak EventSystem uzerinden TUM tiklamalari tek yerden yakaliyoruz -
// boylece hicbir buton "sessiz" kalmiyor, sonradan eklenen butonlar da
// otomatik kapsanmis oluyor.
[RequireComponent(typeof(AudioSource))]
public class GlobalButtonSfx : MonoBehaviour
{
    [SerializeField] AudioClip clickSound;
    [Range(0f, 1f)] [SerializeField] float volume = 0.6f;

    AudioSource _audioSource;
    static readonly List<RaycastResult> _results = new List<RaycastResult>();

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    // Time.timeScale=0 iken de (Pause/LevelUp/GameOver ekranlari) calismasi
    // gerekiyor - Update zaten gercek zamanda (unscaled) calistigi icin sorun yok.
    void Update()
    {
        if (clickSound == null) return;
        if (EventSystem.current == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        _results.Clear();
        EventSystem.current.RaycastAll(ped, _results);

        for (int i = 0; i < _results.Count; i++)
        {
            if (_results[i].gameObject.GetComponentInParent<Selectable>() != null)
            {
                Sfx.PlayOneShot(_audioSource, clickSound, volume);
                return;
            }
        }
    }
}
