using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
   [SerializeField] Image            iconImage;
    [SerializeField] TextMeshProUGUI  keyText;
    [SerializeField] float            useDuration = 3f;

    bool _isUsed = false;
    public bool IsUsed => _isUsed;
    public event System.Action OnDrainComplete;

    public void SetItem(Sprite icon, string keyLabel)
    {
        if (iconImage)
        {
            iconImage.sprite   = icon;
            iconImage.type     = Image.Type.Filled;
            iconImage.fillMethod  = Image.FillMethod.Horizontal;
            iconImage.fillOrigin  = (int)Image.OriginHorizontal.Right;
            iconImage.fillAmount  = 1f;
            iconImage.color    = Color.white;
        }
        if (keyText) keyText.text = keyLabel;
        _isUsed = false;
    }

    public void UseSlot(float duration = 3f)
  {
     Debug.Log("UseSlot çağrıldı, isUsed: " + _isUsed);
    if (_isUsed) return;
    _isUsed = true;
    useDuration = duration;
    StartCoroutine(DrainRoutine());
   }

    public void ClearSlot()
    {
        if (iconImage)
        {
            iconImage.fillAmount = 1f;
            iconImage.color      = Color.clear;
        }
        if (keyText) keyText.text = "";
        _isUsed = false;
    }

    IEnumerator DrainRoutine()
    {
        float elapsed = 0f;

        while (elapsed < useDuration)
        {
            elapsed += Time.deltaTime;
            if (iconImage)
                iconImage.fillAmount = 1f - (elapsed / useDuration);
            yield return null;
        }
        _isUsed = false;
        ClearSlot();
    }
}
