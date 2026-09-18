using System.Collections;
using UnityEngine;

// Oyun basladiginda (sadece ilk Start'ta - restart'ta TEKRAR gosterilmez,
// PlayerIntroDrop bunu !_skipIntro durumunda cagirir) kisa bir "nasil
// oynanir" ipucu paneli gosterir - birkac saniye durur, sonra kendiliginden
// yavasca kaybolur. Panel varsayilan olarak INACTIVE (kapali) durur,
// Show() cagrilana kadar hic gorunmez.
public class HowToPlayOverlay : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float fadeInDuration = 0.3f;
    [SerializeField] float holdDuration = 6f;
    [SerializeField] float fadeOutDuration = 0.6f;

    Coroutine _routine;

    public void Show(System.Action onDone = null)
    {
        // OMUR BOYU SADECE 1 KEZ - daha once gosterildiyse hic acilmaz.
        if (SaveSystem.HowToPlayShown)
        {
            onDone?.Invoke();
            return;
        }
        SaveSystem.MarkHowToPlayShown();

        gameObject.SetActive(true);
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(onDone));
    }

    IEnumerator ShowRoutine(System.Action onDone)
    {
        if (canvasGroup == null)
        {
            gameObject.SetActive(false);
            onDone?.Invoke();
            yield break;
        }

        canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        onDone?.Invoke();
    }
}
