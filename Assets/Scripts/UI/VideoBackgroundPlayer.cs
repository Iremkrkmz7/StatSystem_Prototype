using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Bir VideoPlayer'i bir UI RawImage uzerinde donguye sokarak oynatir.
// RenderTexture'i kendi olusturur, elle bir RenderTexture asset'i hazirlamaya
// gerek kalmaz. Start ekrani arka planinda kesintisiz loop video icin kullanilir.
[RequireComponent(typeof(VideoPlayer))]
public class VideoBackgroundPlayer : MonoBehaviour
{
    [SerializeField] RawImage targetImage;
    [SerializeField] int textureWidth = 1920;
    [SerializeField] int textureHeight = 1080;

    void Start()
    {
        var player = GetComponent<VideoPlayer>();
        var rt = new RenderTexture(textureWidth, textureHeight, 0);
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;
        player.isLooping = true;
        // Time.timeScale=0 iken (Start ekraninda) bile video oynasin.
        player.playbackSpeed = 1f;

        if (targetImage != null) targetImage.texture = rt;

        // Videonun kendi sesi kapali - ayri bir muzik parcasi (loop) kullanilacak.
        player.audioOutputMode = VideoAudioOutputMode.None;

        player.Play();
    }
}
