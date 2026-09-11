using UnityEngine;

// Tum ses efektlerinin (SFX) MERKEZI hacim kontrolu. Ambiyans/muzik AYRI
// tutuluyor (SettingsUI, menuMusic/ambientAudioSource'un kendi volume'unu
// dogrudan ayarliyor) - AudioListener.volume kullanmiyoruz cunku o TUM sesi
// (muzik dahil) kisip "Effect Sound" slider'inin ambiyansi da kismasina
// sebep oluyordu. Bunun yerine her SFX cagrisi buradan gecer, boylece
// Effect Sound slider'i SADECE efektleri etkiler.
public static class Sfx
{
    public static float Volume = 1f;

    // AudioSource.PlayClipAtPoint yerine - gecici/pozisyonel efektler icin
    // (dusman vurusu, buton tiklama, olum sesi vs.)
    public static void PlayAt(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, Volume * volumeScale);
    }

    // source.PlayOneShot yerine - kendi AudioSource'u olan objeler icin
    // (silah, yetenekler, multi-kill banner'i vs.)
    public static void PlayOneShot(AudioSource source, AudioClip clip, float volumeScale = 1f)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, Volume * volumeScale);
    }
}
