using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Start ekraninin video arka plan KATMANINI kapatir.
//
// Video arka plan artik HIC oynatilmiyor - Start ekraninda da (Loading ekranindaki
// gibi) SABIT arkaplan gorseli duruyor. Sebep: Unity'nin VideoPlayer'i WebGL'de
// (itch.io build'i) desteklenmiyor, orada video hic baslamiyor. Editor'de oynayip
// build'de oynamamasi hem tutarsiz hem de "once gorsel, sonra video" seklinde
// sicrayan bir acilisa sebep oluyordu. Simdi her platformda ayni: video katmani
// kapali, altindaki StartMenuPanel'in kendi arkaplan gorseli gorunur.
[RequireComponent(typeof(VideoPlayer))]
public class VideoBackgroundPlayer : MonoBehaviour
{
    [SerializeField] RawImage targetImage;

    void Start()
    {
        GetComponent<VideoPlayer>().enabled = false;

        // DIKKAT: RawImage'i sadece texture'siz birakmak YETMEZ - Unity, texture'i
        // null olan bir RawImage'i OPAK BEYAZ bir dikdortgen olarak cizer (seffaf
        // DEGIL). Tam ekran oldugu icin de altindaki arkaplan gorselini tamamen
        // ortuyordu ("Start ekrani hep beyaz geliyor" sikayeti tam olarak buydu).
        // Bu yuzden bileseni komple kapatiyoruz.
        if (targetImage != null) targetImage.enabled = false;
    }
}
